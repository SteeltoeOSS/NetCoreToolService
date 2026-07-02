// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

// ReSharper disable NotAccessedPositionalProperty.Global

namespace Steeltoe.NetCoreToolService.Models;

/// <summary>
/// Contains information about a project template.
/// </summary>
/// <param name="Name">
/// The display name of the template.
/// </param>
/// <param name="Languages">
/// The supported languages of the template.
/// </param>
/// <param name="Tags">
/// The template tags.
/// </param>
public readonly record struct TemplateInfo(string Name, string Languages, string Tags);
