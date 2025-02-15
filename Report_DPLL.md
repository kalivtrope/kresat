# DPLL: Adjacency lists vs watched literals
The following results were generated by the command `Kresat benchmark -a dpll datasets/`
(with each dataset placed in its respective folder, such as `datasets/uf50`).
The datasets were taken from [the SATLIB benchmark problems page](https://www.cs.ubc.ca/~hoos/SATLIB/benchm.html). I always benchmarked the entire dataset.

The results are rounded to 4 decimal places and averaged over all instances.

## 3-SAT instances
| Dataset     |     Adjacency avg (sec)  |   Watched avg (sec)    |
| -----       |        :--------:        |    :-------:           |
| [uf50](https://www.cs.ubc.ca/~hoos/SATLIB/Benchmarks/SAT/RND3SAT/descr.html)        |          0.0008          |      0.001             |
| uuf50       |          0.0021          |      0.0025            |
| uf75        |          0.012           |      0.0137            |
| uuf75       |          0.0255          |      0.0299            |
| uf100       |          0.1307          |      0.1481            |
| uuf100      |          0.3805          |      0.4333            |
| uf125       |          1.7535          |      1.9221            |
| uuf125      |          3.9183          |      4.344             |

Turns out the watched literal implementation seems to run slower
than the adjacency list one.
I suspect this is due to the fact that my representation of
watched literals has a higher overhead on short clauses (such as those in 3-SAT)
than adjacency lists do.
The current decision heuristic is also just choosing the smallest undecided variable.

Given that the two implementations may possibly do unit propagation in different order
(the AL literal keeps a list of all its clauses while a WL literal only keeps the watched ones),
the implementations might end up propagating a considerably different amount of literals,
which might affect the performance as well.

## Other SAT instances
| Dataset     |     Adjacency avg (sec)  |   Watched avg (sec)    |
| -----       |        :--------:        |    :-------:           |
| [ais](https://www.cs.ubc.ca/~hoos/SATLIB/Benchmarks/SAT/AIS/descr.html)         |         0.5257           |     0.4243             |
| [flat150](https://www.cs.ubc.ca/~hoos/SATLIB/Benchmarks/SAT/GCP/descr.html)     |     2.5405     |     2.8353   |
|   [qg](https://www.cs.ubc.ca/~hoos/SATLIB/Benchmarks/SAT/QG/qg.descr.html)        |         76.6899          |      22.737            |
| [jnh](https://www.cs.ubc.ca/~hoos/SATLIB/Benchmarks/SAT/DIMACS/JNH/descr.html)         |          0.0679          |      0.0539            |
| [phole](https://www.cs.ubc.ca/~hoos/SATLIB/Benchmarks/SAT/DIMACS/PHOLE/descr.html)       |          29.6665         |     33.9376            |

This is a selection of datasets (from [the same page](https://www.cs.ubc.ca/~hoos/SATLIB/benchm.html))
which managed to finish on my testing machine in a reasonable enough time.

By observing the test inputs we may conclude that watched literals
tend to to dominate adjacency lists on inputs with longer clauses.
