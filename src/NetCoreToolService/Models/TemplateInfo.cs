// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.NetCoreToolService.Models;

/// <summary>
/// Contains information about a Net Core Tool template.
/// </summary>
/// <param name="Name">
/// The name of the template.
/// </param>
/// <param name="Languages">
/// The supported languages of the template.
/// </param>
/// <param name="Tags">
/// The template tags.
/// </param>
public readonly record struct TemplateInfo(string Name, string Languages, string Tags)
{
    /// <inheritdoc />
    public override string ToString()
    {
        return $"[name={Name},languages={Languages},tags={Tags}";
    }
}
