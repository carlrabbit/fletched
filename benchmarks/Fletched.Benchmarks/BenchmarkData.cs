using System;
using System.Collections.Generic;
using Fletched.Core;
using Fletched.Core.Runtime;

namespace Fletched.Benchmarks;

[Fact]
[FactIndex(nameof(BenchPerson.Id))]
[FactIndex(nameof(BenchPerson.City), nameof(BenchPerson.Id))]
public partial record struct BenchPerson(int Id, string Name, string City);

[Fact]
[FactIndex(nameof(BenchCity.Name))]
public partial record struct BenchCity(string Name, string Region);

public static class BenchmarkData
{
    public static EngineContext CreatePeopleContext(
        int personCount,
        int cityCount,
        int edgeCount,
        int selectivitySeed)
    {
        if (personCount < 0)
            throw new ArgumentOutOfRangeException(nameof(personCount));
        if (cityCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(cityCount));

        string[] cities = new string[cityCount];
        for (int i = 0; i < cityCount; i++)
            cities[i] = $"city-{i:D2}";

        var people = new BenchPerson[personCount];
        for (int i = 0; i < personCount; i++)
        {
            int cityIndex = Math.Abs((i * 31 + selectivitySeed * 17) % cityCount);
            people[i] = new BenchPerson(i, $"person-{i:D6}", cities[cityIndex]);
        }

        var cityFacts = new BenchCity[cityCount];
        for (int i = 0; i < cityCount; i++)
            cityFacts[i] = new BenchCity(cities[i], i % 2 == 0 ? "north" : "south");

        var edges = new BenchParentEdge[Math.Max(0, edgeCount)];
        for (int i = 0; i < edges.Length; i++)
            edges[i] = new BenchParentEdge($"node-{i}", $"node-{i + 1}");

        var ctx = new EngineContext();
        ctx.BenchPersons = new FactTable<BenchPerson>(people);
        ctx.BenchCitys = new FactTable<BenchCity>(cityFacts);
        ctx.BenchParentEdges = new FactTable<BenchParentEdge>(edges);
        return ctx;
    }

    public static EngineContext CreateAncestorContext(
        int nodeCount,
        int edgeCount,
        int seed)
    {
        if (nodeCount <= 1)
            throw new ArgumentOutOfRangeException(nameof(nodeCount));
        if (edgeCount < 0)
            throw new ArgumentOutOfRangeException(nameof(edgeCount));

        var edges = new List<BenchParentEdge>(edgeCount);
        for (int i = 0; i < edgeCount; i++)
        {
            int from = Math.Abs((i * 13 + seed) % (nodeCount - 1));
            int to = from + 1 + Math.Abs((i * 7 + seed) % (nodeCount - from - 1));
            edges.Add(new BenchParentEdge($"node-{from}", $"node-{to}"));
        }

        var ctx = new EngineContext();
        ctx.BenchParentEdges = new FactTable<BenchParentEdge>(edges.ToArray());
        return ctx;
    }
}
