namespace QuoteManager.Core.Common
{
    /// <summary>
    /// Result pattern for error handling without exceptions
    /// </summary>
    public class Result
    {
        public bool IsSuccess { get; protected set; }
        public bool IsFailure => !IsSuccess;
        public string Error { get; protected set; } = string.Empty;
        public List<string> Errors { get; protected set; } = new();

        protected Result(bool isSuccess, string error)
        {
            IsSuccess = isSuccess;
            Error = error;
            if (!string.IsNullOrEmpty(error))
            {
                Errors.Add(error);
            }
        }

        protected Result(bool isSuccess, List<string> errors)
        {
            IsSuccess = isSuccess;
            Errors = errors;
            Error = errors.FirstOrDefault() ?? string.Empty;
        }

        public static Result Success() => new Result(true, string.Empty);
        public static Result Failure(string error) => new Result(false, error);
        public static Result Failure(List<string> errors) => new Result(false, errors);

        public static Result<T> Success<T>(T value) => new Result<T>(value, true, string.Empty);
        public static Result<T> Failure<T>(string error) => new Result<T>(default, false, error);
        public static Result<T> Failure<T>(List<string> errors) => new Result<T>(default, false, errors);
    }

    public class Result<T> : Result
    {
        public T? Value { get; private set; }

        protected internal Result(T? value, bool isSuccess, string error) : base(isSuccess, error)
        {
            Value = value;
        }

        protected internal Result(T? value, bool isSuccess, List<string> errors) : base(isSuccess, errors)
        {
            Value = value;
        }
    }
}
