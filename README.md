# KreSAT - User documentation

KreSAT is a simple CDCL Solver written in C#.
It has the following features:

- watched literals
- clause learning via 1-UIP
- clause deletion via LBD
- restarts based on the Luby sequence

## Dependencies
- [.NET 8.0](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- [System.Commandline](https://www.nuget.org/packages/System.CommandLine) library (installed automatically while building the project)

## Building the project
You may build the project by running the following command in the root of this repository: 
```
dotnet publish -o bin
```
You may then execute the resulting binary as `bin/Kresat`.

## Usage
The program has three main commands: `tseitin`, `solve` and `benchmark`.
### Tseitin encoding
The `tseitin` command implements the features described in [the first task](https://ktiml.mff.cuni.cz/~kucerap/satsmt/practical/task_tseitin.php).

#### Example input
`(or (not x0) (and x0 (and x1 (not x1))))`
#### Example output
```
c 1 = x0
c 2 = x1
c 3 = _0 ≡ -x0 OR _1 (root)
c 4 = _1 ≡ x0 AND _2
c 5 = _2 ≡ x1 AND -x1
p cnf 5 6
-5 2 0
-5 -2 0
-4 1 0
-4 5 0
-3 -1 4 0
3 0
```

For more details see help:
```
% bin/Kresat tseitin -h
Description:
  Convert formula to CNF using Tseitin transformation

Usage:
  Kresat tseitin [<input> [<output>]] [options]

Arguments:
  <input>   The input file
  <output>  The output file

Options:
  -e, --use-equivalences  Use equivalences instead of => implications [default: False]
  -?, -h, --help          Show help and usage information
```

### Solving
The `solve` command  implements the [second](https://ktiml.mff.cuni.cz/~kucerap/satsmt/practical/task_dpll.php), [third](https://ktiml.mff.cuni.cz/~kucerap/satsmt/practical/task_watched.php) and [fourth task](https://ktiml.mff.cuni.cz/~kucerap/satsmt/practical/task_cdcl.php).

The most basic usage of this command is
```
bin/Kresat solve input.cnf
```
which solves the file `input.cnf` with CDCL on watched literals.

You may also configure the program to solve a file with a plain DPLL strategy or with adjacency lists.
There are also three extra parameters effective on CDCL: the Luby unit run constant (for clause restarts),
initial cache size and cache size multiplier (for clause deletion).

#### Example input (dimacs, `-a dpll`)
```
p cnf 5 6
-5 2 0
-5 -2 0
-4 1 0
-4 5 0
-3 -1 4 0
3 0
```
#### Example output (dimacs, `-a dpll`)
```
SAT -1 2 3 -4 -5
# of decisions: 2, # of propagated vars: 6
Elapsed time: 0.0073578
```
#### Example input (smtlib, `-a dpll`)
`(or a1 (and a2 (and a3 (and a4 a5))))`
#### Example output (smtlib, `-a dpll`)
```
SAT a1 a2 a3 a4 a5
# of decisions: 1, # of propagated vars: 8
Elapsed time: 0.0062579
```
Note that in this case, the solver translates the variable names
back to their original names and outputs them in lexicographical ordering.

For more details on the possible options please consult the help:
```
% bin/Kresat solve -h
Description:
  Solve a given formula

Usage:
  Kresat solve [<input> [<output>]] [options]

Arguments:
  <input>   The input file
  <output>  The output file

Options:
  -f, --format <dimacs|smtlib>                Specify input file format
                                              Alternatively use -c (for smtlib) or -s (for dimacs)
  -c                                          Use smtlib format
  -s                                          Use dimacs format
  -u, --unit-propagation <adjacency|watched>  Select unit propagation data structure [default: watched]
  -a, --strategy <cdcl|dpll>                  Select solving strategy [default: cdcl]
  -z, --cache-size <cache-size>               Set initial cache size (only used in CDCL) [default: 10000]
  -m, --multiplier <multiplier>               Set cache size multiplier (only used in CDCL) [default: 1.1]
  -r, --unit-run <unit-run>                   Set Luby unit run constant (only used in CDCL) [default: 100]
  -?, -h, --help                              Show help and usage information
```

### Benchmarks
The `benchmark` command benchmarks one of the strategies (cdcl/dpll) on both unit propagation data structures (adjacency lists / watched literals) as required by the [third](https://ktiml.mff.cuni.cz/~kucerap/satsmt/practical/task_watched.php) and [fourth task](https://ktiml.mff.cuni.cz/~kucerap/satsmt/practical/task_cdcl.php).
The command expects a path to the folder containing the datasets to benchmark.
It then computes the average run time of instances in each dataset and rounds it up to 4 decimal places.

For example let's say we call `bin/Kresat benchmark datasets`.
Then `datasets` is expected to have the following structure:
```
datasets
├── dir1
│   └── 01.cnf
├── dir2
│   └── 02.sat
├── dir3
│   └── 03.sat
└── dir4
    ├── 04.cnf
    └── 05.sat
```
And the output may then look like this:
```
Dataset          Adjacency avg (sec)     Watched avg (sec)
dir1     0.0183          0.0124
dir2     0.0167          0.0147
dir3     0.0685          0.0535
dir4     0.5301          0.4166
```
It is important to note that you may not specify the input format explicitly
in this command (as opposed to `solve`) -- it has to be inferred purely from the file extension.

For more details see this command's help:
```
% bin/Kresat benchmark -h
Description:
  Run benchmarks

Usage:
  Kresat benchmark [<path to datasets>] [options]

Arguments:
  <path to datasets>  path to dataset folder

Options:
  -a, --strategy <cdcl|dpll>     Select solving strategy [default: cdcl]
  -z, --cache-size <cache-size>  Set initial cache size (only used in CDCL) [default: 10000]
  -m, --multiplier <multiplier>  Set cache size multiplier (only used in CDCL) [default: 1.1]
  -r, --unit-run <unit-run>      Set Luby unit run constant (only used in CDCL) [default: 100]
  -?, -h, --help                 Show help and usage information
```
## Resulting benchmarks
- [DPLL report](./Report_DPLL.md)
- [CDCL report](./Report_CDCL.md)
