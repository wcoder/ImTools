// The source repository
// https://github.com/dadhi/ImTools/blob/zio_from_scratch/test/ImTools.UnitTests/Playground/ZIO.cs

// Parts:
// [X] 1 - https://youtu.be/wsTIcHxJMeQ 
// [X] 2 - https://youtu.be/g8Tuqldu2AE
// [WIP] 3 - https://youtu.be/0IU9mGO_9Rw

// TODO:
// [X] Stack safety / the stack is big though - I got the stack overflow on repating the Do(() => WriteLine("x")) 87253 times
// [ ] Make it work with async/await see how it is done in https://github.com/yuretz/FreeAwait/tree/master/src/FreeAwait
// [ ] Performance
// [ ] Work around the Task.Run or SynchronizaionContext
// [ ] Error handling
// [ ] Environment

using System;
using System.Threading;
//using System.Threading.Tasks;
//using System.Runtime.CompilerServices;
using static System.Console;
using static ImTools.UnitTests.Playground.Z;

namespace ImTools.UnitTests.Playground
{
	public interface ZErased {}

	public interface Z<out A> : ZErased
    {
        void Run(Action<A> consume);
    }
	
	public abstract record ZImpl<A> : Z<A>
	{
		public void Run(Action<A> consume)
		{
			var z = this.Then(a => Z.Do(() => consume(a)));
			
			// todo: @api await the fiber context - so make it awaitable
			new ZFiberContext<Empty>(z);
		}
	} 

	interface ZVal 
	{
		object Value { get; }
	}

	public sealed record ZVal<A>(A Value) : ZImpl<A>, ZVal 
	{
		object ZVal.Value => Value;
	}

	interface ZLazy 
	{
		Func<object> GetValue { get; }
	}

	public sealed record ZLazy<A>(Func<A> GetValue) : ZImpl<A>, ZLazy
	{
		Func<object> ZLazy.GetValue => () => GetValue();
	}

	interface ZThen
	{
		ZErased Za { get; }
		Func<object, ZErased> Cont { get; }
	}

    public sealed record ZThen<A, B>(Z<A> Za, Func<A, Z<B>> Cont) : ZImpl<B>, ZThen
    {
		ZErased ZThen.Za => Za;
		Func<object, ZErased> ZThen.Cont => a => Cont((A)a);
    }

	public sealed record ZAsync<A>(Action<Action<A>> Act) : ZImpl<A>;
	
	public interface ZFiber<out A>
	{
		Z<A> Join();
		//Z<Empty> Interrupt() => throw new NotImplementedException("todo");
	}

	sealed record ZFiberContext<A> : ZFiber<A>
	{	
		public ZErased Za { get; private set; }
		public ZFiberContext(Z<A> za)
		{
			Za = za;
			RunLoop(); //Task.Run(RunLoop);
		}
					
		abstract record EvalState;
		sealed record Done(A Value) : EvalState;
		sealed record Callbacks(Action<A> Act, Callbacks Rest) : EvalState;	
		EvalState _state;
		
		sealed record ContStack(Func<object, ZErased> Cont, ContStack Rest);
		ContStack _stack;
		
		void Complete(A a)
		{
			var s = Interlocked.Exchange(ref _state, new Done(a));
			if (s is Callbacks (var act, var rest))
				for (; act != null; act = rest.Act)
					act(a);
		}
		
		void Await(Action<A> callback)
		{
			var spinWait = new SpinWait();
			while (true)
			{
				var s = _state;
				if (s is Done done)
				{
					callback(done.Value);
					break;
				}
				
				var callbacks = new Callbacks(callback, s as Callbacks);
				if (Interlocked.CompareExchange(ref _state, callbacks, s) == s)
					break;

				spinWait.SpinOnce();
			}
		}
		
		public Z<A> Join() => Z.Async<A>(Await);
		
		void RunLoop()
		{
			var loop = true;
			while (loop)
			{
				switch (Za)
				{
					case ZVal or ZLazy: 
					{
						var val = Za is ZVal v ? v.Value : ((ZLazy)Za).GetValue();
						if (_stack is ContStack(var cont, var rest)) 
						{
							Za = cont(val);
							_stack = rest;
						}
						else 
						{
							Complete((A)val);
							loop = false;
						}
						break;
					}
					case ZThen t:
					{
						Za = t.Za;
						_stack = new ContStack(t.Cont, _stack);
						break;
					}
					case ZAsync<A> ac:
					{
						loop = false;
						if (_stack == null)
							ac.Act(Complete);
						else
							ac.Act(a => 
							{
								Za = a.Val();
								RunLoop();
							});
						break;
					}
				}
			}
		}
	}
		
    public static class Z
    {
		public sealed record Empty 
		{
			public override string ToString() => "(empty)";
		}
		public static readonly Empty empty = default(Empty);
			
        public static Z<A> Val<A>(this A a) => new ZVal<A>(a);
        public static Z<A> Get<A>(Func<A> getA) => new ZLazy<A>(getA);
		public static Z<Empty> Do(Action act) => new ZLazy<Empty>(() => { act(); return empty; });
        public static Z<A> Async<A>(Action<Action<A>> act) => new ZAsync<A>(act); // TODO @wip convert Action<Action<A>> to more general Func<Func<A, ?>, ?> or provide the separate case class 

		/// <summary>This is Bind, SelectMany or FlatMap... but I want to be unique and go with Then for now as it seems to have a more precise meaning IMHO</summary>
        public static Z<B> Then<A, B>(this Z<A> za, Func<A, Z<B>> from) => new ZThen<A, B>(za, from);

        public static Z<B> To<A, B>(this Z<A> za, Func<A, B> map) => za.Then(a => map(a).Val());
        public static Z<B> ToVal<A, B>(this Z<A> za, B b) => za.Then(_ => b.Val()); // TODO @perf optimize allocations
        public static Z<B> ToGet<A, B>(this Z<A> za, Func<B> getB) => za.Then(_ => getB().Val()); // TODO @perf optimize allocations

        public static Z<(A, B)> Zip<A, B>(this Z<A> za, Z<B> zb) => za.Then(a => zb.Then(b => Val((a, b))));		
		public static Z<C> ZipWith<A, B, C>(this Z<A> za, Z<B> zb, Func<A, B, C> zip) => za.Then(a => zb.Then(b => zip(a, b).Val()));
		public static Z<A> And<A, B>(this Z<A> za, Z<B> zb) => za.Then(a => zb.Then(_ => a.Val()));
		public static Z<A> RepeatN<A>(this Z<A> za, int n) => n <= 1 ? za : za.And(za.RepeatN(n - 1));
		
        public static Z<ZFiber<A>> Fork<A>(this Z<A> za) => Get(() => new ZFiberContext<A>(za));
		
        public static Z<(A, B)> ZipPar<A, B>(this Z<A> za, Z<B> zb) => 
			from af in za.Fork()
			from b in zb
			from a in af.Join()
			select (a, b);		
    }

    public static class ZLinq
    {
        public static Z<R> Select<A, R>(this Z<A> za, Func<A, R> selector) => za.To(selector);
        public static Z<R> SelectMany<A, R>(this Z<A> za, Func<A, Z<R>> next) => za.Then(next);
        public static Z<R> SelectMany<A, B, R>(this Z<A> za, Func<A, Z<B>> getZb, Func<A, B, R> project) =>
            za.Then(a => getZb(a).Then(b => project(a, b).Val()));
    }
	    
    public class Tests
    {
		int _id;
		int Id() => Interlocked.Increment(ref _id);   
		
        public Z<string> Map_small() =>
           	Z.Val(42).To(x => x + "!");

        public Z<int> Async_sleep() =>
            Async<int>(run =>
			{
				var id = Id();
                WriteLine($"Sleep for 300 - {id}");
                Thread.Sleep(300);
                WriteLine($"Woken {id}");
                run(42);
            });

		
		public Z<int> Get_sleep() =>
            Get(() => 
			{
				var id = Id();
                WriteLine($"Sleep for 300 - {id}");
                Thread.Sleep(300);
				WriteLine($"Woken - {id}");
                return 43;
            });
		
		public Z<int> Async_seq() =>
            from _ in Z.Do(() => WriteLine("Before Async_seq.."))
			from a in Async_sleep()
			from b in Async_sleep()
			from _1 in Z.Do(() => WriteLine("After Async_seq"))
			select a + b;
		
		public Z<int> Get_seq() =>
            from _ in Z.Do(() => WriteLine("Before Get_seq.."))
			from a in Get_sleep()
			from b in Get_sleep()
			from _1 in Z.Do(() => WriteLine("After Get_seq"))
			select a + b + 1;
		
		public Z<int> Async_fork() =>
            from _ in Z.Do(() => WriteLine("Before Async_fork.."))
			from fa in Async_sleep().Fork()
			from fb in Async_sleep().Fork()
			from a in fa.Join()
			from b in fb.Join()
			from _1 in Z.Do(() => WriteLine("After Async_fork"))
			select a + b + 2;
		
		public Z<int> Zip_par() =>
            from _ in Z.Do(() => WriteLine("Before ZipPar.."))
			from x in Z.ZipPar(Async_sleep(), Async_sleep())
			from _1 in Z.Do(() => WriteLine("After ZipPar"))
			select x.Item1 + x.Item2 + 3;

		public Z<int> Async_counter()
		{
			var i = 0;
            return 
				from b in Z.Do(() => WriteLine("Before Async_counter.."))
				from x in Z.Do(() => Interlocked.Increment(ref i)).Fork().RepeatN(100)
				from w in Z.Do(() => Thread.Sleep(500))
				from a in Z.Do(() => WriteLine("After Async_counter and sleep for 500"))
				select i;
		}

		public Z<Empty> Repeat(int n) 
		{
			var i = 0; 
			return Z.Do(() => WriteLine("HOWDY " + (++i))).RepeatN(n);
		}
    }
	
	public class Program
	{
		public static void ZMain()
		{
			var t = new Tests();
			
			void run<A>(Z<A> z, string name = "") { WriteLine(name + " >> "); z.Run(x => WriteLine(x)); Write("\n"); }

			run(t.Async_counter(), nameof(t.Async_counter));
			
			run(t.Map_small(),   nameof(t.Map_small));

			run(t.Repeat(3),     nameof(t.Repeat));
			//run(t.Repeat(15000), nameof(t.Repeat)); // should not StackOverflow

		//	run(t.Async_sleep(), nameof(t.Async_sleep));
			//run(t.Async_seq(),   nameof(t.Async_seq));
			
			run(t.Async_fork(),  nameof(t.Async_fork));
			//WriteLine("Major sleep for 1000");
			//Thread.Sleep(1000);

			//run(t.Get_sleep(),   nameof(t.Get_sleep));
			//run(t.Get_seq(),     nameof(t.Get_seq));
		
			run(t.Zip_par(),     nameof(t.Zip_par));
			// don't need to sleep here because par executes the right on the current thread
			
			WriteLine("==DONE==");
		}
	}
}