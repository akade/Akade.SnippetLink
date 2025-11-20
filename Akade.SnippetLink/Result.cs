namespace Akade.SnippetLink;

public abstract record Result<T>
{
    private Result() { }

    public sealed record Success(T Value) : Result<T>;
    public sealed record Failure(string ErrorMessage) : Result<T>
    {
        public static implicit operator Result.Failure(Failure value) => new(value.ErrorMessage);
    }

    public static implicit operator Result<T>(T value) => new Success(value);

    public static implicit operator Result<T>(Result.Failure value) => new Failure(value.ErrorMessage);
}

public abstract record Result
{
    private Result() { }

    public sealed record Success : Result;
    public sealed record Failure(string ErrorMessage) : Result;


}