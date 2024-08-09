# KreSAT - Developer documentation

This overview is meant to describe the project structure.

Let us walk through a normal program execution.

First off, the program *parses its command-line arguments*.
The CLI logic is kept in [Program.cs](./src/Kresat/Program.cs).

Then the program parses one or more files
in their respective format (which is either SmtLib or Dimacs).
Parsing is split into two parts: *scanning* (which tokenizes the input)
and then *parsing* itself, which converts the tokens into a *common representation*.
Scanning logic resides in [Scanners/](./src/Kresat/Scanners) and parsing logic
is in [Parsers/](./src/Kresat/Parsers).

Each format has its own scanner and parser.
In the end, files in either format end up being converted to
a [`CommonRepresentation`](./src/Kresat/Representations/CommonRepresentation.cs),
which is just a 2D list of integers with some additional metadata.

Once in a common representation, the *solving strategy* along with the
*unit propagation data structure* is instantiated.
There are currently two solving strategies: CDCL and DPLL, both residing in [Solvers/](./src/Kresat/Solvers).
The unit propagation data structures reside in [DataStructures/](./src/Kresat/DataStructures).

After a model is found, it is *verified* for correctness.
This is done by the class in [Verifiers/](./src/Kresat/Verifiers).
Currently there is only a very simple verifier able to check
whether a reported model satisfied the original formula.
The code may however potentially be extended in the future
to verify unsatisfiability as well
(if it ever gets added).

If anything goes wrong during the program execution,
then a very minimalistic error *logger* is informed.
The program exits prematurely if it finds out an error
somewhere in the code has occurred.
The logger is defined in [Loggers/](./src/Kresat/Loggers).

In the event of a *benchmark*, the code in [Benchmarks/](./src/Kresat/Benchmarks)
get called. Currently the code is capable of benchmarking both strategies (cdcl/dpll)
on both unit propagation data structures (watched/adjacency).

## Advanced C\# features
Here's a quick overview of the used advanced C\# features taught in the NPRG038 course:

- static abstract interface methods: in [IClause](./src/Kresat/DataStructures/IClause.cs) and in [Token](./src/Kresat/Scanners/Token.cs).
In both cases this feature is used to enforcing a multiparametric constructor on generic types.
- generic classes and interfaces: in all of [DataStructures](./src/Kresat/DataStructures) and [Scanners](./src/Scanners)
- extension methods: [ListExtensions for UnitPropagation](./src/Kresat/DataStructures/UnitPropagationDS.cs#L141-L158)
- LINQ: [elegant model verifying](./src/Kresat/Verifiers/Verifier.cs), [benchmarks](./src/Kresat/Benchmarks/UnitPropagation.cs) and [model printing](./src/Kresat/Solvers/Verdict.cs)
