using System;
using System.Collections.Generic;

using Reusable.DataModels;
using Xunit;

namespace Reusable.DataAccess.Text.UnitTests
{
    public class CsvParserTest
    {
        [Fact]
        public void Initialize_WhenPropertyCannotBeSet_ThenThrow()
        {
            CsvParser<MyClassWithInvalidAccess> parser;
            var exception = Assert.Throws<TypeInitializationException>(() => { parser = new(); });
            Assert.IsType<Common.ParserException>(exception.InnerException);
        }

        [Fact]
        public void Parse_WhenMissingHeader_ThenThrow()
        {
            CsvParser<Person> parser = new();
            Assert.Throws<Common.ParserException>(
                () => parser.Parse(Array.Empty<string>()).GetEnumerator().MoveNext());
        }

        [Fact]
        public void Parse_WhenHeaderEmpty_ThenThrow()
        {
            CsvParser<Person> parser = new();
            var exception = Assert.Throws<AggregateException>(
                () => parser.Parse(new string[] { "" }).GetEnumerator().MoveNext());

            Assert.All(exception.InnerExceptions, ex => Assert.IsType<Common.ParserException>(ex));
            Assert.Equal(3, exception.InnerExceptions.Count);
        }

        [Fact]
        public void Parse_WhenColumnMissing_ThenThrow()
        {
            CsvParser<Person> parser = new();
            var exception = Assert.Throws<AggregateException>(
                () => parser.Parse(new string[] { "id" }).GetEnumerator().MoveNext());

            Assert.All(exception.InnerExceptions, ex => Assert.IsType<Common.ParserException>(ex));
            Assert.Equal(2, exception.InnerExceptions.Count);
        }

        [Fact]
        public void Parse_WhenDuplicatedColumns_ThenThrow()
        {
            CsvParser<Person> parser = new();
            Assert.Throws<Common.ParserException>(
                () => parser.Parse(new string[] { "id,nome,nome,vivo" }).GetEnumerator().MoveNext());
        }

        [Fact]
        public void Parse_WhenTableEmpty_ThenReturnNothing()
        {
            CsvParser<Person> parser = new();
            IEnumerable<Person> items = parser.Parse(new string[] { "id,nome,vivo" });
            Assert.Empty(items);
        }

        [Fact]
        public void Parse_WhenNumberColumnsDiffers_ThenThrow()
        {
            CsvParser<Person> parser = new();
            var exception = Assert.Throws<AggregateException>(
                () => parser.Parse(new string[] {
                    "id,nome,vivo",
                    "10,Lenin",
                    "11,Trotzki"
                }).GetEnumerator().MoveNext());

            Assert.All(exception.InnerExceptions, ex => Assert.IsType<Common.ParserException>(ex));
            Assert.Equal(2, exception.InnerExceptions.Count);
        }

        [Fact]
        public void Parse_WhenTableWellFormed_ThenReturnParsedObjects()
        {
            CsvParser<Person> parser = new();
            IEnumerable<Person> actualItems =
                parser.Parse(new string[] {
                    "id,nome,vivo",
                    "10,Putin,true",
                    "11,Lenin,false",
                });

            Person[] expectedItems = new[] {
                new Person { Id = 10, Name = "Putin", Alive = true },
                new Person { Id = 11, Name = "Lenin", Alive = false },
            };
            Assert.Equal(expectedItems, actualItems);
        }

        [Fact]
        public void Parse_WhenConversionNotPossible_IfSomeLinesArValid_ThenProduceParsedObjectsBeforeThrow()
        {
            CsvParser<Person> parser = new();
            IEnumerable<Person> actualItems =
                parser.Parse(new string[] {
                    "id,nome,vivo",
                    "07,Trotzki,não",
                    "08,,false",
                    ",Rosa,false",
                    "9a,Kautsky,false",
                    "10,Putin,true",
                });

            try
            {
                Assert.Contains(new Person { Id = 8, Name = "", Alive = false }, actualItems);
                Assert.Contains(new Person { Id = 10, Name = "Putin", Alive = true }, actualItems);
            }
            catch (Exception ex)
            {
                Assert.IsType<AggregateException>(ex);
                var ax = (AggregateException)ex;
                Assert.Equal(3, ax.InnerExceptions.Count);
                Assert.All(ax.InnerExceptions, ix => Assert.IsType<Common.ParserException>(ix));
            }
        }
    }

    #region Types under test

    class MyClassWithInvalidAccess
    {
        [Field(Name = "Id")]
        public int Id { get; private set; }
    }

    class Person : IEquatable<Person>
    {
        [Field(Name = "Id")]
        public int Id { get; set; }

        [Field(Name = "Nome")]
        public string? Name { get; set; }

        [Field(Name = "Vivo")]
        public bool Alive { get; set; }

        public int Age { get; set; }

        public bool Equals(Person? other)
        {
            if (other != null)
            {
                return this == other ||
                    (Id == other.Id && Name == other.Name && Age == other.Age);
            }

            return false;
        }
    }

    #endregion
}