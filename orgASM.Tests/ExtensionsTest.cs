using orgASM;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace orgASM.Tests
{
    
    
    /// <summary>
    ///This is a test class for ExtensionsTest and is intended
    ///to contain all ExtensionsTest Unit Tests
    ///</summary>
    [TestClass()]
    public class ExtensionsTest
    {
        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion


        /// <summary>
        ///A test for TrimComments
        ///</summary>
        [TestMethod()]
        public void TrimCommentsTest()
        {
            string value = "\tHello, world! \"; string\" '; char' ; Actual comment";
            string expected = "Hello, world! \"; string\" '; char'";
            string actual;
            actual = Extensions.TrimComments(value);
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        ///A test for TrimExcessWhitespace
        ///</summary>
        [TestMethod()]
        public void TrimExcessWhitespaceTest()
        {
            string value = "\t Hello   ,world  ! \"Excess    in a string\"   ' and a char   '  ";
            string expected = "Hello ,world ! \"Excess    in a string\" ' and a char   '";
            string actual;
            actual = Extensions.TrimExcessWhitespace(value);
            Assert.AreEqual(expected, actual);
        }
    }
}
