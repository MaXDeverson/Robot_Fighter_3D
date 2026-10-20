using System.Collections.Generic;
using NUnit.Framework;

namespace Game.Events.Tests
{
    [TestFixture]
    internal sealed class EventPathTests
    {
        [Test]
        public void SplitsIntoSegments()
        {
            EventPath path = new EventPath("Level.Entered.Boss");

            Assert.That(path.SegmentCount, Is.EqualTo(3));
            Assert.That(path.GetSegment(0), Is.EqualTo("Level"));
            Assert.That(path.GetSegment(2), Is.EqualTo("Boss"));
            Assert.That(path.Full, Is.EqualTo("Level.Entered.Boss"));
            Assert.That(path.IsRoot, Is.False);
        }

        [Test]
        public void EmptyWildcardAndDefaultAreTheRoot()
        {
            Assert.That(new EventPath(null).IsRoot, Is.True);
            Assert.That(new EventPath(string.Empty).IsRoot, Is.True);
            Assert.That(new EventPath("*").IsRoot, Is.True);
            Assert.That(default(EventPath).IsRoot, Is.True);
            Assert.That(default(EventPath).Full, Is.EqualTo(string.Empty));
        }

        [Test]
        public void RootMatchesEverything()
        {
            EventPath root = new EventPath("*");

            Assert.That(new EventPath("Level.Entered").IsUnder(root), Is.True);
            Assert.That(new EventPath("Player.Died").IsUnder(root), Is.True);
            Assert.That(default(EventPath).IsUnder(root), Is.True);
        }

        [Test]
        public void BranchMatchesItselfAndDescendants()
        {
            EventPath branch = new EventPath("Level");

            Assert.That(new EventPath("Level").IsUnder(branch), Is.True);
            Assert.That(new EventPath("Level.Entered").IsUnder(branch), Is.True);
            Assert.That(new EventPath("Level.Entered.Boss").IsUnder(branch), Is.True);
        }

        [Test]
        public void BranchComparesSegmentsNotStringPrefixes()
        {
            EventPath branch = new EventPath("Level");

            Assert.That(new EventPath("LevelUp.Something").IsUnder(branch), Is.False,
                "'Level' must not swallow 'LevelUp' just because one string starts with the other.");
            Assert.That(new EventPath("Levels.Entered").IsUnder(branch), Is.False);
        }

        [Test]
        public void DeeperBranchDoesNotMatchShallowerPath()
        {
            Assert.That(new EventPath("Level").IsUnder(new EventPath("Level.Entered")), Is.False);
        }

        [Test]
        public void TrailingWildcardSegmentIsStripped()
        {
            EventPath branch = new EventPath("Level.*");

            Assert.That(branch.Full, Is.EqualTo("Level"));
            Assert.That(new EventPath("Level.Entered").IsUnder(branch), Is.True);
            Assert.That(new EventPath("LevelUp.Gained").IsUnder(branch), Is.False);
        }

        [TestCase("Level..Entered")]
        [TestCase(".Level")]
        [TestCase("Level.")]
        [TestCase("1Level")]
        [TestCase("Level.Ent ered")]
        [TestCase("Level-Entered.X")]
        public void MalformedPathsThrowWithTheOffendingText(string path)
        {
            EventPathFormatException exception = Assert.Throws<EventPathFormatException>(
                () => { EventPath unused = new EventPath(path); });

            Assert.That(exception.Message, Does.Contain(path));
        }

        [Test]
        public void RegistryReadsTheAttributeOnceAndCachesIt()
        {
            EventPathRegistry registry = new EventPathRegistry();

            EventPath first = registry.GetPath(typeof(SuiteLevelEntered));
            EventPath second = registry.GetPath(typeof(SuiteLevelEntered));

            Assert.That(first.Full, Is.EqualTo("Suite.Level.Entered"));
            Assert.That(second.Equals(first), Is.True);
            Assert.That(registry.KnownPaths.ContainsKey(typeof(SuiteLevelEntered)), Is.True);
        }

        [Test]
        public void RegistryRejectsAnEventWithoutAnAttribute()
        {
            EventPathRegistry registry = new EventPathRegistry();

            EventPathFormatException exception = Assert.Throws<EventPathFormatException>(
                () => registry.GetPath(typeof(SuiteUnpathed)));

            Assert.That(exception.Message, Does.Contain("EventPath"));
            Assert.That(exception.Message, Does.Contain(nameof(SuiteUnpathed)));
        }

        [Test]
        public void RegistryRejectsAMalformedAttribute()
        {
            EventPathRegistry registry = new EventPathRegistry();

            EventPathFormatException exception = Assert.Throws<EventPathFormatException>(
                () => registry.GetPath(typeof(SuiteMalformed)));

            Assert.That(exception.Message, Does.Contain(nameof(SuiteMalformed)));
        }

        [Test]
        public void RegistryRejectsTwoTypesClaimingOnePath()
        {
            EventPathRegistry registry = new EventPathRegistry();
            registry.GetPath(typeof(SuiteDuplicateA));

            EventPathConflictException exception = Assert.Throws<EventPathConflictException>(
                () => registry.GetPath(typeof(SuiteDuplicateB)));

            Assert.That(exception.Message, Does.Contain("Suite.Duplicate"));
            Assert.That(exception.Message, Does.Contain(nameof(SuiteDuplicateA)));
            Assert.That(exception.Message, Does.Contain(nameof(SuiteDuplicateB)));
        }

        [Test]
        public void ValidateReportsEveryProblemAtOnce()
        {
            EventPathRegistry registry = new EventPathRegistry();
            List<string> problems = new List<string>();

            bool clean = registry.TryValidate(
                new[]
                {
                    typeof(SuiteLevelEntered),
                    typeof(SuiteUnpathed),
                    typeof(SuiteDuplicateA),
                    typeof(SuiteDuplicateB),
                    typeof(SuiteMalformed)
                },
                problems);

            Assert.That(clean, Is.False);
            Assert.That(problems.Count, Is.EqualTo(3), "unpathed, duplicate and malformed");
        }

        [Test]
        public void RegistryCachesBranchesToo()
        {
            EventPathRegistry registry = new EventPathRegistry();

            EventPath branch = registry.GetBranch("Suite.Level");
            EventPath again = registry.GetBranch("Suite.Level");

            Assert.That(branch.Equals(again), Is.True);
            Assert.That(registry.GetBranch("*").IsRoot, Is.True);
            Assert.That(registry.GetBranch(null).IsRoot, Is.True);
        }
    }
}
