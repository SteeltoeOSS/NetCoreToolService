// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.NetCoreToolService.Models
{
    /// <summary>
    /// Contains information about a Net Core Tool template.
    /// </summary>
    public class TemplateInfo
    {
        /// <summary>
        /// Gets the name of the template.
        /// </summary>
        public string Name { get; internal init; }

        /// <summary>
        /// Gets the supported languages of the template.
        /// </summary>
        public string Languages { get; internal init; }

        /// <summary>
        /// Gets the template tags.
        /// </summary>
        public string Tags { get; internal init; }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"[name={Name},languages={Languages},tags={Tags}";
        }
    }
}
