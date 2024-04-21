using FocusVolumeControl.Overrides;

namespace FocusVolumeControl.UnitTests
{
	public class OverrideParserTests
	{
		[Theory]
		[InlineData(null)]
		[InlineData("")]
		[InlineData(" ")]
		[InlineData("\n")]
		public void BlankReturnsEmpty(string str)
		{
			//arrange

			//act
			var overrides = OverrideParser.Parse(str);

			//assert
			Assert.Empty(overrides);
		}

		[Fact]
		public void HelldiversParses()
		{
			//arrange
			var str =
				"""
				eq: HELLDIVERS™ 2
				helldivers2
				""";

			//act
			var overrides = OverrideParser.Parse(str);

			//assert
			Assert.Single(overrides);
			Assert.Equal(MatchType.Equal, overrides[0].MatchType);
			Assert.Equal("HELLDIVERS™ 2", overrides[0].WindowQuery);
			Assert.Equal("helldivers2", overrides[0].AudioProcessName);
		}

		[Fact]
		public void MultipleOverridesParse()
		{
			//arrange
			var str =
				"""
				eq: HELLDIVERS™ 2
				helldivers2

				start: Task
				Steam
				""";

			//act
			var overrides = OverrideParser.Parse(str);

			//assert
			Assert.Equal(MatchType.Equal, overrides[0].MatchType);
			Assert.Equal("HELLDIVERS™ 2", overrides[0].WindowQuery);
			Assert.Equal("helldivers2", overrides[0].AudioProcessName);
			Assert.Equal(MatchType.StartsWith, overrides[1].MatchType);
			Assert.Equal("Task", overrides[1].WindowQuery);
			Assert.Equal("Steam", overrides[1].AudioProcessName);
		}

		[Fact]
		public void IncompleteMatchesAreSkipped()
		{
			//arrange
			var str =
				"""
				eq: HELLDIVERS™ 2

				start: Task
				Steam
				""";

			//act
			var overrides = OverrideParser.Parse(str);

			//assert
			Assert.Single(overrides);
			Assert.Equal(MatchType.StartsWith, overrides[0].MatchType);
			Assert.Equal("Task", overrides[0].WindowQuery);
			Assert.Equal("Steam", overrides[0].AudioProcessName);
		}

		[Fact]
		public void InvalidMatchesAreSkipped()
		{
			//arrange
			var str =
				"""
				equal: Chrome
				chrome

				end: Task
				Steam
				""";

			//act
			var overrides = OverrideParser.Parse(str);

			//assert
			Assert.Single(overrides);
			Assert.Equal(MatchType.EndsWith, overrides[0].MatchType);
			Assert.Equal("Task", overrides[0].WindowQuery);
			Assert.Equal("Steam", overrides[0].AudioProcessName);

		}

		[Fact]
		public void MatchesAreCaseInsensitive()
		{
			//arrange
			var str =
				"""
				Eq: 0
				0

				eNd: 1
				1

				StArT: 2
				2

				Regex: 3
				3
				""";

			//act
			var overrides = OverrideParser.Parse(str);

			//assert
			Assert.Equal(MatchType.Equal, overrides[0].MatchType);
			Assert.Equal(MatchType.EndsWith, overrides[1].MatchType);
			Assert.Equal(MatchType.StartsWith, overrides[2].MatchType);
			Assert.Equal(MatchType.Regex, overrides[3].MatchType);
		}

		[Fact]
		public void CommentsAreSkipped()
		{
			//arrange
			var str =
				"""
				//Eq: 0
				//0

				end: 1
				1
				""";

			//act
			var overrides = OverrideParser.Parse(str);

			//assert
			Assert.Single(overrides);
			Assert.Equal(MatchType.EndsWith, overrides[0].MatchType);
			Assert.Equal("1", overrides[0].WindowQuery);
			Assert.Equal("1", overrides[0].AudioProcessName);
		}


	}
}