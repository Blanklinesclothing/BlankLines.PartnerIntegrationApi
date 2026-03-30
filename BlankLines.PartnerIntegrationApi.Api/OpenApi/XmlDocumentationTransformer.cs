using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Xml.XPath;

namespace BlankLines.PartnerIntegrationApi.Api.OpenApi;

internal sealed class XmlDocumentationTransformer : IOpenApiOperationTransformer
{
    private readonly XPathNavigator? _navigator;

    public XmlDocumentationTransformer()
    {
        var xmlFile = Path.Combine(AppContext.BaseDirectory,
            $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");

        if (!File.Exists(xmlFile))
        {
            return;
        }

        var document = new XPathDocument(xmlFile);
        _navigator = document.CreateNavigator();
    }

    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (_navigator is null)
        {
            return Task.CompletedTask;
        }

        if (context.Description.ActionDescriptor is not ControllerActionDescriptor descriptor)
        {
            return Task.CompletedTask;
        }

        var memberName = GetMemberName(descriptor.MethodInfo);
        var node = _navigator.SelectSingleNode($"/doc/members/member[@name='{memberName}']");

        if (node is null)
        {
            return Task.CompletedTask;
        }

        var summary = node.SelectSingleNode("summary")?.Value?.Trim();
        if (!string.IsNullOrEmpty(summary))
        {
            operation.Summary = summary;
        }

        var remarks = node.SelectSingleNode("remarks")?.Value?.Trim();
        if (!string.IsNullOrEmpty(remarks))
        {
            operation.Description = remarks;
        }

        return Task.CompletedTask;
    }

    private static string GetMemberName(MethodInfo method)
    {
        var declaringType = method.DeclaringType!.FullName;
        var parameters = method.GetParameters();

        if (parameters.Length == 0)
        {
            return $"M:{declaringType}.{method.Name}";
        }

        var paramList = string.Join(",", parameters.Select(p => p.ParameterType.FullName));
        return $"M:{declaringType}.{method.Name}({paramList})";
    }
}
