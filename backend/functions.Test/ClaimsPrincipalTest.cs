using System.Security.Claims;
using functions.Claims;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Xunit;

namespace functions.Test.Claims
{
    // Create a FunctionContext Mock based on the FunctionContext class
    public class FunctionContextMock : FunctionContext
    {
        //
        // Summary:
        //     Gets the invocation ID. This identifier is unique to an invocation.
        public override string InvocationId { get; } = "FunctionContextMock";

        //
        // Summary:
        //     Gets the function ID, typically assigned by the host. This identifier is unique
        //     to a function and stable across invocations.
        public override string FunctionId { get; } = "FunctionContextMock";

        //
        // Summary:
        //     Gets the distributed tracing context.
        public override TraceContext TraceContext { get; }

        //
        // Summary:
        //     Gets the binding context for the current function invocation. This context is
        //     used to retrieve binding data.
        public override BindingContext BindingContext { get; }

        //
        // Summary:
        //     Gets the retry context containing information about retry acvitity for the event
        //     that triggered the current function invocation.
        public override RetryContext RetryContext { get; }

        //
        // Summary:
        //     Gets or sets the System.IServiceProvider that provides access to this execution's
        //     services.
        public override IServiceProvider InstanceServices { get; set; }

        //
        // Summary:
        //     Gets the Microsoft.Azure.Functions.Worker.FunctionContext.FunctionDefinition
        //     that describes the function being executed.
        public override FunctionDefinition FunctionDefinition { get; }

        //
        // Summary:
        //     Gets or sets a key/value collection that can be used to share data within the
        //     scope of this invocation.
        public override IDictionary<object, object> Items { get; set; }

        //
        // Summary:
        //     Gets a collection containing the features supported by this context.
        public override IInvocationFeatures Features { get; }
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        public FunctionContextMock()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        {
        }
    }
    // Create an HttpRequestData Mock based on the HttpRequestData class
    public class HttpRequestDataMock : HttpRequestData
    {
        //
        // Summary:
        //     A System.IO.Stream containing the HTTP body data.
        public override Stream Body { get; }

        //
        // Summary:
        //     Gets a Microsoft.Azure.Functions.Worker.Http.HttpHeadersCollection containing
        //     the request headers.
        public override HttpHeadersCollection Headers { get; } = [];

        //
        // Summary:
        //     Gets an System.Collections.Generic.IReadOnlyCollection`1 containing the request
        //     cookies.
        public override IReadOnlyCollection<IHttpCookie> Cookies { get; }

        //
        // Summary:
        //     Gets the System.Uri for this request.
        public override Uri Url { get; }

        //
        // Summary:
        //     Gets an System.Collections.Generic.IEnumerable`1 containing the request identities.
        public override IEnumerable<ClaimsIdentity> Identities { get; }

        //
        // Summary:
        //     Gets the HTTP method for this request.
        public override string Method { get; }
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        public HttpRequestDataMock() : base(new FunctionContextMock())
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        {
        }

        public override HttpResponseData CreateResponse()
        {
            throw new NotImplementedException();
        }
    }
    public class ClaimsPrincipalParserTests
    {
        [Fact]
        public void Parse_Fails_WhenUserRolesIsEmpty()
        {
            // Arrange
            var httpRequestData = new HttpRequestDataMock();

            Assert.Throws<InvalidOperationException>(() => ClaimsPrincipalParser.Parse(httpRequestData));
        }

        [Fact]
        public void Parse_ReturnsClaimsPrincipalWithIdentity_WhenUserRolesIsNotEmpty()
        {
            // Arrange
            var httpRequestData = new HttpRequestDataMock();
            httpRequestData.Headers.Add("x-ms-client-principal", "eyJVc2VySWQiOiJVc2VyMSIsIlVzZXJEZXRhaWxzIjoiZGV0YWlscyIsIklkZW50aXR5UHJvdmlkZXIiOiJteVByb3ZpZGVyIiwiVXNlclJvbGVzIjpbImFub255bW91cyIsImF1dGhlbnRpY2F0ZWQiLCJhZG1pbiJdfQ==");

            // Act
            var result = ClaimsPrincipalParser.Parse(httpRequestData);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Identities);

            var identity = result.Identities.FirstOrDefault();
            Assert.Equal("details", identity?.Name);
            Assert.Equal("details", identity?.FindFirst(ClaimTypes.Name)?.Value);
            Assert.Equal("User1", identity?.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            Assert.Equal(2,identity?.FindAll(ClaimTypes.Role).Count());
        }
    }
}