// <copyright file="Result.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace SurveyPro.Application.Common;

/// <summary>
/// Represents the result of an operation.
/// </summary>
public class Result
{
    protected Result(bool isSuccess, string error)
    {
        this.IsSuccess = isSuccess;
        this.Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !this.IsSuccess;

    public string Error { get; }

    public static implicit operator Result(string error)
    {
        return Failure(error);
    }

    public static Result Success()
    {
        return new Result(true, string.Empty);
    }

    public static Result Failure(string error)
    {
        return new Result(false, error);
    }
}

/// <summary>
/// Represents the result of an operation with a value.
/// </summary>
/// <typeparam name="TValue">Value type.</typeparam>
public sealed class Result<TValue> : Result
{
    private Result(TValue? value, bool isSuccess, string error)
        : base(isSuccess, error)
    {
        this.Value = value;
    }

    public TValue? Value { get; }

    public static implicit operator Result<TValue>(TValue value)
    {
        return Success(value);
    }

    public static implicit operator Result<TValue>(string error)
    {
        return Failure(error);
    }

    public static Result<TValue> Success(TValue value)
    {
        return new Result<TValue>(value, true, string.Empty);
    }

    public static new Result<TValue> Failure(string error)
    {
        return new Result<TValue>(default, false, error);
    }
}
