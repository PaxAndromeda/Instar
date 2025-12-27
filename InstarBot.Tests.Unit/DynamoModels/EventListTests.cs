using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using PaxAndromeda.Instar.DynamoModels;
using Serilog;
using Xunit;

namespace InstarBot.Tests.DynamoModels;

public class EventListTests
{
    [Fact]
    public void Add_SequentialItems_ShouldBeAddedInOrder()
    {
        TestUtilities.SetupLogging();
        
        // Arrange/Act
        //var list = new EventList<TestEntry<int>>();
        var list = new SortedSet<TestEntry<int>>(Comparer<TestEntry<int>>.Create((x, y) => x.Date.CompareTo(y.Date)))
        {
            new(1, DateTime.Now - TimeSpan.FromMinutes(2)),
            new(2, DateTime.Now - TimeSpan.FromMinutes(1)),
            new(3, DateTime.Now)
        };

        // Assert
        var collapsedList = list.ToList();
        collapsedList[0].Value.Should().Be(1);
        collapsedList[1].Value.Should().Be(2);
        collapsedList[2].Value.Should().Be(3);
    }
    
    [Fact]
    public void Add_IntermediateItem_ShouldBeAddedInMiddle()
    {
        TestUtilities.SetupLogging();
        
        // Arrange
        var list = new EventList<TestEntry<int>>
        {
            new(1, DateTime.Now - TimeSpan.FromMinutes(2)),
            new(3, DateTime.Now)
        };
        
        list.First().Value.Should().Be(1);

        // Act
        Log.Information("Inserting entry 2 at the middle of the list.");
        list.Add(new TestEntry<int>(2, DateTime.Now - TimeSpan.FromMinutes(1)));
        
        // Assert
        var collapsedList = list.ToList();
        collapsedList[0].Value.Should().Be(1);
        collapsedList[1].Value.Should().Be(2); // this should be in the middle
        collapsedList[2].Value.Should().Be(3);
    }
    
    [Fact]
    public void Add_LastItem_ShouldBeAddedInMiddle()
    {
        TestUtilities.SetupLogging();
        
        // Arrange
        var list = new EventList<TestEntry<int>>
        {
            new(3, DateTime.Now),
            new(2, DateTime.Now - TimeSpan.FromMinutes(1))
        };
        
        list.Latest()?.Value.Should().Be(3);
        list.First().Value.Should().Be(2);

        // Act
        Log.Information("Inserting entry 1 at the end of the list.");
        list.Add(new TestEntry<int>(1, DateTime.Now - TimeSpan.FromMinutes(2)));
        
        // Assert
        var collapsedList = list.ToList();
        collapsedList[0].Value.Should().Be(1);
        collapsedList[1].Value.Should().Be(2); // this should be in the middle
        collapsedList[2].Value.Should().Be(3);
    }
    
    [Fact]
    public void Add_RandomItems_ShouldBeChronological()
    {
        TestUtilities.SetupLogging();
        
        // Arrange
        var items = new List<TestEntry<int>>();
        for (var i = 0; i < 100; i++)
            items.Add(new TestEntry<int>(i, DateTime.Now - TimeSpan.FromMinutes(i)));
        
        // Run a Fisher-Yates shuffle:
        var rng = new Random();
        var n = items.Count;
        while (n > 1)
        {
            n--;
            var k = rng.Next(n + 1);
            (items[k], items[n]) = (items[n], items[k]);
        }
        
        // Arrange
        var list = new EventList<TestEntry<int>>(items);
        
        // Act
        Log.Information("Inserting entry 1 at the end of the list.");
        list.Add(new TestEntry<int>(1, DateTime.Now - TimeSpan.FromMinutes(2)));
        
        // Assert
        var collapsedList = list.ToList();

        collapsedList.Should().BeInDescendingOrder(d => d.Value);
    }
    
    private class TestEntry<T>(T value, DateTime date) : ITimedEvent
    {
        public DateTime Date { get; } = date;
        public T Value { get; } = value;
    }
}