using Google.Protobuf;
using Grpc.Core;
using Grpc.Core.Interceptors;
using ProtoValidate;

namespace Contracts.Grpcx;

public interface IRequestValidator
{
    bool CanValidate(IMessage message);

    IEnumerable<string> Validate(IMessage message);
}

public sealed class ValidationInterceptor(Validator validator, IEnumerable<IRequestValidator> customValidators) : Interceptor
{
    private readonly IRequestValidator[] _custom = customValidators.ToArray();

    public override Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        if (request is IMessage message)
        {
            var errors = Errors(message);
            if (errors.Count > 0)
                throw new RpcException(new Status(StatusCode.InvalidArgument,
                    "validation error: " + string.Join("; ", errors)));
        }

        return continuation(request, context);
    }

    private List<string> Errors(IMessage message)
    {
        var custom = Array.Find(_custom, v => v.CanValidate(message));
        if (custom is not null)
            return custom.Validate(message).ToList();

        var result = validator.Validate(message, failFast: false);
        return result.IsSuccess ? [] : result.Violations.Select(Describe).ToList();
    }

    private static string Describe(Buf.Validate.Violation violation)
    {
        var path = violation.Field is null
            ? ""
            : string.Join(".", violation.Field.Elements.Select(e => e.FieldName));
        return string.IsNullOrEmpty(path) ? violation.Message : $"{path}: {violation.Message}";
    }
}
