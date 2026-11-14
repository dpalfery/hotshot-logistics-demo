// <copyright file="DatabaseCollection.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace HotshotLogistics.Tests.Utils.TestHelpers
{
    /// <summary>
    ///     Collection every test class that reads or writes the shared SQL Server database joins, so those classes
    ///     run one after another inside this test assembly. Separate test projects run in separate processes and
    ///     are not serialised by this; they stay safe through unique per-test ids and scoped assertions.
    /// </summary>
    [CollectionDefinition(Name)]
    public sealed class DatabaseCollection
    {
        /// <summary>
        ///     The collection name used in <c>[Collection(DatabaseCollection.Name)]</c>.
        /// </summary>
        public const string Name = "DatabaseCollection";
    }
}
