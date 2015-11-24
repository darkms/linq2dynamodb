﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Linq2DynamoDb.DataContext.Tests.Entities;
using Linq2DynamoDb.DataContext.Tests.Helpers;
using NUnit.Framework;

namespace Linq2DynamoDb.DataContext.Tests.QueryTests
{
    public abstract class BasicQueryTestsCommon : DataContextTestBase
	{
		// ReSharper disable InconsistentNaming
        [Test]
        public void DataContext_Find_ReturnsExistingRecordWhenUsedWithHashAndRangeKeys()
        {
            var book = BooksHelper.CreateBook(publishYear: 1);
            BooksHelper.CreateBook(book.Name, book.PublishYear + 1);

            var bookTable = Context.GetTable<Book>();
            var storedBook = bookTable.Find(book.Name, book.PublishYear);

            Assert.AreEqual(book.Name, storedBook.Name, "Book with wrong name was returned");
            Assert.AreEqual(book.PublishYear, storedBook.PublishYear, "Book with wrong publishYear was returned");
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "Sequence contains no elements")]
        public void DataContext_Find_ThrowsExceptionWhenRecordDoesNotExist()
        {
            var bookTable = Context.GetTable<Book>();
            bookTable.Find(Guid.NewGuid().ToString(), 0);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "has 2 key fields, but 1 key values was provided", MatchType = MessageMatch.Contains)]
        public void DataContext_Find_ThrowsExceptionIfUserDoesNotProvideRangeKeyInHashRangeTables()
        {
            var book = BooksHelper.CreateBook();

            var bookTable = Context.GetTable<Book>();
            bookTable.Find(book.Name);
        }

		[Test]
		public void DataContext_Query_ReturnsEnumFieldsStoredAsInt()
		{
			var book = BooksHelper.CreateBook(userFeedbackRating: Book.Stars.Platinum);

			var bookTable = Context.GetTable<Book>();
			var booksQuery = from record in bookTable where record.Name == book.Name select record;
			var storedBook = booksQuery.First();

			Assert.AreEqual(book.UserFeedbackRating, storedBook.UserFeedbackRating);
		}

		[Test]
		public void DataContext_Query_ReturnsEnumFieldsStoredAsString()
		{
			var book = BooksHelper.CreateBook(popularityRating: Book.Popularity.Average);

			var bookTable = Context.GetTable<Book>();
			var booksQuery = from record in bookTable where record.Name == book.Name select record;
			var storedBook = booksQuery.First();

			Assert.AreEqual(book.PopularityRating, storedBook.PopularityRating);
		}

		[Test]
		public void DataContext_Query_ReturnsStringFields()
		{
			var book = BooksHelper.CreateBook(author: "TestAuthor");

			var bookTable = Context.GetTable<Book>();
			var booksQuery = from record in bookTable where record.Name == book.Name select record;
			var storedBook = booksQuery.First();

			Assert.AreEqual(book.Author, storedBook.Author);
		}

		[Test]
		public void DataContext_Query_ReturnsIntFields()
		{
			var book = BooksHelper.CreateBook(numPages: 555);

			var bookTable = Context.GetTable<Book>();
			var booksQuery = from record in bookTable where record.Name == book.Name select record;
			var storedBook = booksQuery.First();

			Assert.AreEqual(book.NumPages, storedBook.NumPages);
		}

		[Test]
		public void DataContext_Query_ReturnsDateTimeFields()
		{
			var testTime = DateTime.Today.Add(new TimeSpan(0, 8, 45, 30, 25));

			var book = BooksHelper.CreateBook(lastRentTime: testTime);

			var bookTable = Context.GetTable<Book>();
			var booksQuery = from record in bookTable where record.Name == book.Name select record;
			var storedBook = booksQuery.First();

			Assert.AreEqual(book.LastRentTime.ToUniversalTime(), storedBook.LastRentTime.ToUniversalTime());
		}

		[Test]
		public void DataContext_Query_ReturnsListArrays()
		{
			var book = BooksHelper.CreateBook(rentingHistory: new List<string> { "Marie", "Anna", "Alex" });

			var bookTable = Context.GetTable<Book>();
			var booksQuery = from record in bookTable where record.Name == book.Name select record;
			var storedBook = booksQuery.First();

			Assert.IsNotNull(storedBook.RentingHistory, "Expected non-null string array");
			var expectedSequence = string.Join(", ", book.RentingHistory.OrderBy(s => s));
            var actualSequence = string.Join(", ", storedBook.RentingHistory.OrderBy(s => s));
			Assert.AreEqual(expectedSequence, actualSequence, "String array elements sequence incorrect");
		}

        [Test]
        public void DataContext_Query_ReturnsComplexObjectProperties()
        {
            var book = BooksHelper.CreateBook(publisher: new Book.PublisherDto { Title = "O’Reilly Media", Address = "Sebastopol, CA"});

            var bookTable = Context.GetTable<Book>();
            var booksQuery = from record in bookTable where record.Name == book.Name select record;
            var storedBook = booksQuery.First();

            Assert.AreEqual(book.Publisher.ToString(), storedBook.Publisher.ToString(), "Complex object properties are not equal");
        }

        [Test]
        public void DataContext_Query_ReturnsComplexObjectListProperties()
        {
            var book = BooksHelper.CreateBook(reviews: new List<Book.ReviewDto> { new Book.ReviewDto { Author = "Beavis", Text = "Cool" }, new Book.ReviewDto { Author = "Butt-head", Text = "This sucks!" } });

            var bookTable = Context.GetTable<Book>();
            var booksQuery = from record in bookTable where record.Name == book.Name select record;
            var storedBook = booksQuery.First();

            var expectedSequence1 = string.Join(", ", book.ReviewsList.Select(r=>r.ToString()).OrderBy(s => s));
            var actualSequence1 = string.Join(", ", storedBook.ReviewsList.Select(r => r.ToString()).OrderBy(s => s));
            Assert.AreEqual(expectedSequence1, actualSequence1, "Complex object list properties are not equal");
        }

		[Test]
		public void DataContext_Query_ReturnsDictionaryStringTimeSpan()
		{
			var book =
				BooksHelper.CreateBook(
					filmsBasedOnBook:
						new Dictionary<string, TimeSpan>
						{
							{ "A Man and a Test", TimeSpan.FromMinutes(90) },
							{ "Avatar 12", TimeSpan.FromMinutes(9000) },
							{ "Aliens 8", TimeSpan.FromMinutes(80) }
						});

			var bookTable = Context.GetTable<Book>();
			var booksQuery = from record in bookTable where record.Name == book.Name select record;
			var storedBook = booksQuery.First();

			Assert.IsNotNull(storedBook.FilmsBasedOnBook, "Expected non-null dictionary");
			Assert.AreEqual(book.FilmsBasedOnBook.Count, storedBook.FilmsBasedOnBook.Count, "Dictionary size mismatch");
			foreach (var key in book.FilmsBasedOnBook.Keys)
			{
				Assert.IsTrue(
					storedBook.FilmsBasedOnBook.ContainsKey(key),
					"Stored dictionary does not have required key (" + key + ")");
				Assert.AreEqual(book.FilmsBasedOnBook[key], storedBook.FilmsBasedOnBook[key], "Values mismatch");
			}
		}

		[Test]
		public void DataContext_Query_ReturnsUninitializedFields()
		{
			var book = BooksHelper.CreateBook(publishYear: 2013);

			var bookTable = Context.GetTable<Book>();
			var booksQuery = from record in bookTable where record.Name == book.Name select record;
			var storedBook = booksQuery.First();

			Assert.AreEqual(book.NumPages, storedBook.NumPages, "Incorrect default value for int field");
			Assert.AreEqual(book.PopularityRating, storedBook.PopularityRating, "Incorrect default value for enum (string) field");
			Assert.AreEqual(book.UserFeedbackRating, storedBook.UserFeedbackRating, "Incorrect default value for enum (int) field");
			Assert.AreEqual(book.LastRentTime.ToUniversalTime(), storedBook.LastRentTime.ToUniversalTime(), "Incorrect default value for DateTime field");
            Assert.IsNull(storedBook.RentingHistory, "Incorrect default value for IList<string> field");
			Assert.IsNull(storedBook.FilmsBasedOnBook, "Incorrect default value for IDictionary<string, TimeSpan> field");
		}

		[Test]
		public void DataContext_Query_ReturnsCorrectRecordIfPositionedByRangeKey()
		{
			var bookRev1 = BooksHelper.CreateBook(publishYear: 2012);
			var bookRev2 = BooksHelper.CreateBook(bookRev1.Name, 2013);

			var bookTable = Context.GetTable<Book>();
			var booksQuery = from record in bookTable
				where record.Name == bookRev1.Name && record.PublishYear == bookRev2.PublishYear
				select record;

			Assert.AreEqual(1, booksQuery.Count());

			var storedBook = booksQuery.First();

			Assert.AreEqual(bookRev2.Name, storedBook.Name, "Returned record does not contain the required HashKey");
			Assert.AreEqual(bookRev2.PublishYear, storedBook.PublishYear, "Returned record does not contain the required RangeKey");
		}

		// GroupBy operation is currently not supported
		[Test]
		[Ignore]
		public void DateContext_Query_GroupByReturnsGrouppedRecords()
		{
			var bookRev1 = BooksHelper.CreateBook(publishYear: 2012);
			var bookRev2 = BooksHelper.CreateBook(bookRev1.Name, 2013);

			var bookTable = Context.GetTable<Book>();
			var booksGroupsByName = from record in bookTable
									where record.Name == bookRev1.Name
									group record by record.Name
										into recordGroup
										select recordGroup;

			Assert.AreEqual(1, booksGroupsByName.Count());

			var firstGroup = booksGroupsByName.First();

			Assert.IsTrue(firstGroup.Contains(bookRev1, new BooksComparer()));
			Assert.IsTrue(firstGroup.Contains(bookRev2, new BooksComparer()));
		}

		[Test]
		public void DateContext_Query_DefaultIfEmptyReturnsCollectionWithOneNullRecordIfNoDefaultValueSpecified()
		{
			var bookTable = Context.GetTable<Book>();
			var booksQuery = from record in bookTable where record.Name == Guid.NewGuid().ToString() select record;

			Assert.AreEqual(0, booksQuery.Count());

			var defaultIfEmptyResult = booksQuery.DefaultIfEmpty();
			Assert.AreEqual(1, defaultIfEmptyResult.Count());

			Assert.IsNull(defaultIfEmptyResult.First());
		}

		[Test]
		public void DateContext_Query_DefaultIfEmptyReturnsCollectionWithOneDefaultRecordIfDefaultValueSpecified()
		{
			var bookTable = Context.GetTable<Book>();
			var booksQuery = from record in bookTable where record.Name == Guid.NewGuid().ToString() select record;

			Assert.AreEqual(0, booksQuery.Count());

			var defaultBook = new Book { Name = Guid.NewGuid().ToString(), PublishYear = 1999 };
			var defaultIfEmptyResult = booksQuery.DefaultIfEmpty(defaultBook);
			Assert.AreEqual(1, defaultIfEmptyResult.Count());

			var actualResult = defaultIfEmptyResult.First();

			Assert.IsNotNull(actualResult);
			Assert.AreEqual(defaultBook.Name, actualResult.Name);
			Assert.AreEqual(defaultBook.PublishYear, actualResult.PublishYear);
		}

		[Test]
		public void DateContext_Query_AnyFunctionReturnsTrueIfOneElementMatchesPredicate()
		{
			var bookRev1 = BooksHelper.CreateBook(publishYear: 2012);
			var bookRev2 = BooksHelper.CreateBook(bookRev1.Name, 2013);

			var anyResult = Context.GetTable<Book>().Any(book => book.Name == bookRev1.Name && book.PublishYear == bookRev2.PublishYear);

			Assert.IsTrue(anyResult);
		}

		[Test]
		public void DateContext_Query_AnyFunctionReturnsTrueIfAllElementsMatchPredicate()
		{
			var bookRev1 = BooksHelper.CreateBook(publishYear: 2012);
			BooksHelper.CreateBook(bookRev1.Name, 2013);

			var anyResult = Context.GetTable<Book>().Any(book => book.Name == bookRev1.Name);

			Assert.IsTrue(anyResult);
		}

		[Test]
		public void DateContext_Query_AnyFunctionReturnsFalseIfNoneElementsMatchPredicate()
		{
			var bookRev1 = BooksHelper.CreateBook(publishYear: 2012);
			BooksHelper.CreateBook(bookRev1.Name, 2013);

			var anyResult = Context.GetTable<Book>().Any(book => book.Name == Guid.NewGuid().ToString());

			Assert.IsFalse(anyResult);
		}

		[Test]
		public void DateContext_Query_AllFunctionReturnsTrueIfAllElementsMatchPredicate()
		{
			var bookRev1 = BooksHelper.CreateBook(publishYear: 2012);
			BooksHelper.CreateBook(bookRev1.Name, 2013);

			var bookTable = Context.GetTable<Book>();
			var booksQuery = from record in bookTable where record.Name == bookRev1.Name select record;

			var allResult = booksQuery.All(book => book.Name == bookRev1.Name);
			Assert.IsTrue(allResult);
		}

		[Test]
		public void DateContext_Query_AllFunctionReturnsFalseIfAtLeastOneDoesNotMatchPredicate()
		{
			var bookRev1 = BooksHelper.CreateBook(publishYear: 2012, numPages: 123);
			BooksHelper.CreateBook(bookRev1.Name, 2013, numPages: bookRev1.NumPages);
			BooksHelper.CreateBook(bookRev1.Name, 2014, numPages: 124);

			var allResult = Context.GetTable<Book>().All(book => book.Name == bookRev1.Name && book.NumPages == bookRev1.NumPages);

			Assert.IsFalse(allResult);
		}

		[Test]
		public void DateContext_Query_AllFunctionReturnsFalseIfNoneElementsMatchPredicate()
		{
			var bookRev1 = BooksHelper.CreateBook(publishYear: 2012);
			BooksHelper.CreateBook(bookRev1.Name, 2013);

			var allResult = Context.GetTable<Book>().All(book => book.Name == Guid.NewGuid().ToString());

			Assert.IsFalse(allResult);
		}

		[Test]
		public void DateContext_Query_CountFunctionReturnsCorrectNumberOfRecordsOnSmallDataSets()
		{
			const int DataSetLength = 20;
			var bookRev1 = BooksHelper.CreateBook();
			Parallel.For(1, DataSetLength, i => BooksHelper.CreateBook(bookRev1.Name, bookRev1.PublishYear + i));

			var numberOfRecordsInDb = Context.GetTable<Book>().Count(book => book.Name == bookRev1.Name);

			Assert.AreEqual(DataSetLength, numberOfRecordsInDb);
		}
		
		[Test]
		public void DateContext_Query_LongCountFunctionReturnsCorrectNumberOfRecordsOnSmallDataSets()
		{
			const int DataSetLength = 20;
			var bookRev1 = BooksHelper.CreateBook();
			Parallel.For(1, DataSetLength, i => BooksHelper.CreateBook(bookRev1.Name, bookRev1.PublishYear + i));

			var numberOfRecordsInDb = Context.GetTable<Book>().LongCount(book => book.Name == bookRev1.Name);

			Assert.AreEqual(DataSetLength, numberOfRecordsInDb);
		}

		// ReSharper restore InconsistentNaming
	}
}
