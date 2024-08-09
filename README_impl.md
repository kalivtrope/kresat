# KreSAT - Implementation notes
## Decision heuristic
The decision heuristic is as simple as can be
-- the solver always chooses the lowest unassigned variable.

## Unit propagation
Since I was supposed to implement two unit propagation algorithms,
I decided to factor out the common code for unit propagation and
I put it into a common generic class `UnitPropagationDS`, as can be seen in [DataStructures/UnitPropagationDS.cs](./src/Kresat/DataStructures/UnitPropagationDS.cs).
This class is later extended to a `UnitPropagationDSWithLearning` to support learning and all things that come with it - finding the 1-UIP, clause deletion and restarts. The relevant code may be found in [DataStructures/UnitPropagationDSWithLearning.cs](./src/Kresat/DataStructures/UnitPropagationDSWithLearning.cs).
### Clause and literal representation
Both clauses and literals are implemented as reference types
and accessed mostly via references rather than numeric IDs.

While I'm aware of the fact that my design might significantly affect the performance (which it probably did),
I find this way easier and less error-prone than manipulating
a couple of global arrays around and passing references to them across the classes.

### Adjacency lists
Adjacency lists are really simple in its nature: when a literal
is satisfied or falsified,
then the clause containing the literal in question (or its negation)
just increment a counter.
The clause is thus able to tell if it is unit in O(1) time by simply
comparing two numbers.

This laziness then bites back when it comes to retrieving the unit literal, for which
the clause has to traverse all of its literals to actually figure the answer out.

The implementation may be found in
[DataStructures/AdjacencyLists.cs](./src/Kresat/DataStructures/AdjacencyLists.cs).

### Watched literals
Watched literals are based on the fact that we're mostly only interested whether a given clause
is unit or falsified.
To detect whether the clause is unit we keep two unsatisfied literal witnesses (watches) which are there to prove that the clause indeed isn't unit.
Whenever a watch is falsified, we need to find another watch in the clause -- if there is no other unsatisfied literal left in the clause, the watch just stays at its place and is later used as a proof of the clause being falsified.

I tried several approaches to watched literals -- namely having a separate class for Watches
and maintaining the watched literals' indices in them (see [WatchedLiterals@e948c08bbc](https://gitea.ks.matfyz.cz/auburn/kresat/src/commit/e948c08bbc53ba85a3bcd57d6cdae672570aecb0/src/Kresat/DataStructures/WatchedLiterals.cs#L13-L120) for more details).

However, I found the following approach more efficient: the clause always watches
its first two literals and then swaps them accordingly if it has to replace them.
The current approach may be seen in [DataStructures/WatchedLiterals.cs](./src/Kresat/DataStructures/WatchedLiterals.cs).

## Conflict analysis
My implementation of conflict analysis is based on the approach described in
Handbook of Satisfiability, 2nd edition, pages 140-144.

The algorithm can be found in [DataStructures/UnitPropagationDSWithLearning.cs](./src/Kresat/DataStructures/UnitPropagationDSWithLearning.cs#L71-L129)

## Clause deletion
I used the LBD metric (Literals Block Distance; number of distinct decision levels of literals -- less is better)
to measure the activity of learnt clauses.

Once the cache size is full, half of the less active clauses are deleted
and the cache size is multiplied by a small constant `>1`.

This is a fairly expensive operation, so I tried to waste as
little time as possible in the process.
The deleted learnt clauses are marked as deleted. Each deleted clause reports
the literals which need to forget about them
(each literal keeps a list of all containing clauses (in case of adjacency lists)
or clauses with watch (watched literals)).
After marking the clauses as deleted, the program traverses all clauses
of the affected literals and removes references to the clauses if they are supposed to be deleted.

For more details see the implementation in [DataStructures/UnitPropagationDSWithLearning.cs](./src/Kresat/DataStructures/UnitPropagationDSWithLearning.cs#L45-L56).

## Restarts
Restarts are implemented fairly simply -- by just calling `Backtrack(0)` (backtrack to decision level 0).
The important thing is to figure out how often one should restart.

My approach was to restart after `unit_run * l_k` conflicts, where `l_k` is the k-th element of the Luby sequence.
The value of `k` increases after each restart. The value of `unit_run` is a constant by default arbitrarily set to 100.
The implementation of Luby sequences is based on [this OEIS entry](https://oeis.org/A182105) (namely the Formula section).

See [Solvers/CDCL.cs](./src/Kresat/Solvers/CDCL.cs#L56-L79) for the implementation of Luby sequences.
