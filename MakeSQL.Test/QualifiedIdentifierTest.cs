using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MakeSQL.Test
{
    [TestClass]
    public sealed class QualifiedIdentifierTest
    {
        private const string table_name = "the_table";
        private const string schema_name = "dbo";

        private static readonly QualifiedIdentifier ID1 = $"[{schema_name}].[{table_name}]";
        private static readonly QualifiedIdentifier ID2 = $".{table_name}";

        [TestMethod]
        public void FromString()
        {            
            Assert.AreEqual(schema_name, ID1.Schema, "schema name has been lost");
            Assert.AreEqual(table_name, ID1.Identifier, "object name has been lost");

            Assert.AreEqual(null, ID2.Schema, "schema name has been lost");
            Assert.AreEqual(table_name, ID2.Identifier, "object name has been lost");
        }

        [TestMethod]
        public void MakeString()
        {
            QualifiedIdentifier ID3 = ID1.ToString();
            Assert.AreEqual(ID1.Schema, ID3.Schema, "schema name has been lost");
            Assert.AreEqual(ID1.Identifier, ID3.Identifier, "object name has been lost");

            ID3 = ID2.ToString();
            Assert.AreEqual(ID2.Schema, ID3.Schema, "schema name has been lost");
            Assert.AreEqual(ID2.Identifier, ID3.Identifier, "object name has been lost");
        }
    }
}
