using System;

namespace ResultOfTask
{
	public class None
	{
		private None()
		{
		}
	}
	public struct Result<T>
	{
		public string Error { get; }
		internal T Value { get; }

        public bool IsSuccess => Error == null;

        public T GetValueOrThrow()
		{
			if (IsSuccess) return Value;
			throw new InvalidOperationException($"No value. Only Error {Error}");
        }

        public Result(string error, T value = default(T))
        {
            Error = error;
            Value = value;
        }
    }

	public static class Result
	{
		public static Result<T> AsResult<T>(this T value)
		{
			return Ok(value);
		}

		public static Result<T> Ok<T>(T value)
		{
			return new Result<T>(null, value);
		}

		public static Result<T> Fail<T>(string e)
		{
			return new Result<T>(e);
		}

		public static Result<T> Of<T>(Func<T> f, string error = null)
		{
			try
			{
				return Ok(f());
			}
			catch (Exception e)
			{
				return Fail<T>(error ?? e.Message);
			}
		}

		public static Result<TOutput> Then<TInput, TOutput>(
			this Result<TInput> input,
			Func<TInput, TOutput> continuation)
		{
		    if (!input.IsSuccess) return Fail<TOutput>(input.Error);

		    return Of(() => continuation(input.Value));
		}

		public static Result<TOutput> Then<TInput, TOutput>(
			this Result<TInput> input,
			Func<TInput, Result<TOutput>> continuation)
		{
            if (!input.IsSuccess) return Fail<TOutput>(input.Error);

            return continuation(input.Value);
        }

		public static Result<TInput> OnFail<TInput>(
			this Result<TInput> input,
			Action<string> handleError)
		{
		    if (!input.IsSuccess)
		        handleError(input.Error);
            return input;
		}

	    public static Result<T> ReplaceError<T>(
	        this Result<T> input,
	        Func<string, string> errorReplace)
	    {
	        if (input.IsSuccess) return input;

	        return Fail<T>(errorReplace(input.Error));
	    }

	    public static Result<T> RefineError<T>(this Result<T> input, string prefix)
	    {
	        return input.ReplaceError(e => $"{prefix} {e}");
	    }
    }
}