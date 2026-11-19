// <copyright file="HealthResponseWriter.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HotshotLogistics.Api.Health
{
    /// <summary>
    ///     Writes a health report as JSON with the overall status and each check's name and status.
    ///     It never writes descriptions, data or exceptions, so a probe cannot learn internals.
    /// </summary>
    public static class HealthResponseWriter
    {
        /// <summary>
        ///     Writes the report to the response.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        /// <param name="report">The health report.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static async Task WriteAsync(HttpContext context, HealthReport report)
        {
            context.Response.ContentType = "application/json";
            await using Utf8JsonWriter writer = new(context.Response.BodyWriter);
            writer.WriteStartObject();
            writer.WriteString("status", report.Status.ToString());
            writer.WriteStartArray("checks");
            foreach (KeyValuePair<string, HealthReportEntry> entry in report.Entries)
            {
                writer.WriteStartObject();
                writer.WriteString("name", entry.Key);
                writer.WriteString("status", entry.Value.Status.ToString());
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }
    }
}
