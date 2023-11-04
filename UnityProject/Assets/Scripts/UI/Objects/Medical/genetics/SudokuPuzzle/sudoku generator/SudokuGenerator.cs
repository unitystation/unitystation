using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Logs;
using UnityEngine;
using Random = UnityEngine.Random;

public class SudokuGenerator
{
	public int NR_SQUARES = 81;

	public char BLANK_CHAR = '.';
	public string BLANK_BOARD = "...................................................."+
	".............................";

	public string  DIGITS = "123456789";       // Allowed sudoku.DIGITS
	public string  ROWS = "ABCDEFGHI";         // Row lables
	public string  COLS = "123456789";        // Column lables
	public List<string> SQUARES;
	public Dictionary<string, List<List<string>>> SQUARE_UNITS_MAP;
	public Dictionary<string, List<string>> SQUARE_PEERS_MAP;
	public int MIN_GIVENS = 17;            // Minimum number of givens

	public Dictionary<string, int> DIFFICULTY = new Dictionary<string, int>{
		{"easy", 62},
		{"medium",      53},
		{"hard",         44},
		{"very-hard",    35},
		{"insane",      26},
		{"inhuman",      17},
	};

	public SudokuGenerator()
	{
		SQUARES             = _cross(ROWS, COLS);
		var UNITS               = _get_all_units(ROWS, COLS);
		SQUARE_UNITS_MAP    = _get_square_units_map(SQUARES, UNITS);
		SQUARE_PEERS_MAP    = _get_square_peers_map(SQUARES, SQUARE_UNITS_MAP);

	}

	public string generate(string InDIFFICULTY)
	{
		var difficulty = DIFFICULTY[InDIFFICULTY];


		// Get a set of squares and all possible candidates for each square
        var blank_board = new List<char>();
        for(var i = 0; i < NR_SQUARES; ++i){
            blank_board.Add('.');
        }
        var candidates = _get_candidates_map(blank_board);

        // For each item in a shuffled list of squares
        var shuffled_squares = _shuffle(SQUARES);
        foreach(var si in shuffled_squares){
            var square = si;

            // If an assignment of a random chioce causes a contradictoin, give
            // up and try again
            var rand_candidate_idx = _rand_range(candidates[square].Length);
            var rand_candidate = candidates[square][rand_candidate_idx];
            if(_assign(candidates, square, rand_candidate) == null){
                break;
            }

            // Make a list of all single candidates
            var single_candidates = new List<string>();
            foreach(var sAi in SQUARES){
                var square2 = sAi;

                if(candidates[square2].Length == 1){
                    single_candidates.Add(candidates[square2]);
                }
            }

            // If we have at least difficulty, and the unique candidate count is
            // at least 8, return the puzzle!
            if(single_candidates.Count >= difficulty && _strip_dups(single_candidates).Count >= 8){
                var board = "";
                var givens_idxs = new List<int>();
                for (int i = 0; i < SQUARES.Count; i++)
                {
	                var square2 = SQUARES[i];
                    if(candidates[square2].Length == 1){
                        board += candidates[square2];
                        givens_idxs.Add(i);
                    } else {
                        board += BLANK_CHAR;
                    }
                }

                // If we have more than `difficulty` givens, remove some random
                // givens until we're down to exactly `difficulty`
                var nr_givens = givens_idxs.Count;
                if(nr_givens > difficulty){
	                givens_idxs = givens_idxs.Shuffle().ToList();
                    for(var i = 0; i < nr_givens - difficulty; ++i){
                        var target = givens_idxs[i];
                        board = board.Substring(0, target) + BLANK_CHAR +
                            board.Substring(target + 1);
                    }
                }

                // Double check board is solvable
                // TODO: Make a standalone board checker. Solve is expensive.
                if( string.IsNullOrEmpty(solve(board)) == false){
                    return board;
                }
            }
        }

        // Give up and try a new puzzle
        return generate(InDIFFICULTY);
    }


	public Dictionary<string, string> _get_candidates_map(List<char> board){
		/* Get all possible candidates for each square as a map in the form
		{square: sudoku.DIGITS} using recursive constraint propagation. Return `false`
		if a contradiction is encountered
		*/

		// Assure a valid board
		var report = validate_board(board);
		if(report != true){
			return null;
		}

		var candidate_map = new Dictionary<string, string>();
		var squares_values_map = _get_square_vals_map(board);

		// Start by assigning every digit as a candidate to every square
		foreach (var si in SQUARES){
			candidate_map[si] = DIGITS;
		}

		// For each non-blank square, assign its value in the candidate map and
		// propigate.
		foreach(var square in squares_values_map){
			var val = square.Value;
			var key = square.Key;

			if(_in(val, DIGITS)){
				var new_candidates = _assign(candidate_map, key, val);

				// Fail if we can't assign val to square
				if(new_candidates == null){
					return null;
				}
			}
		}

		return candidate_map;
	}

	public List<string> _cross(string a, string b){
		/* Cross product of all elements in `a` and `b`, e.g.,
		sudoku._cross("abc", "123") ->
		["a1", "a2", "a3", "b1", "b2", "b3", "c1", "c2", "c3"]
		*/
		var result = new List<string>();
		foreach (var ai in a)
		{
			foreach(var bi in b){
				result.Add(ai.ToString() + bi.ToString());
			}
		}
		return result;
	}

	public List<List<string>> _get_all_units(string rows, string cols){
		/* Return a list of all units (rows, cols, boxes)
		*/
		var units = new List<List<string>>();

		// Rows
		foreach (var ri in rows){
			units.Add(_cross(ri.ToString(), cols));
		}

		// Columns
		foreach(var ci in cols){
			units.Add(_cross(rows, ci.ToString()));
		}

		// Boxes
		var row_squares = new List<string>(){"ABC", "DEF", "GHI"};
		var col_squares = new List<string>(){"123", "456", "789"};
		foreach(var rsi in row_squares){
			foreach(var csi in col_squares){
				units.Add(_cross(rsi, csi));
			}
		}

		return units;
	}


	public Dictionary<string, List<List<string>>> _get_square_units_map(List<string> squares, List<List<string>> units){
		/* Return a map of `squares` and their associated units (row, col, box)
		*/
		var square_unit_map = new Dictionary<string, List<List<string>>>();

		// For every square...
		foreach(var si in squares){
			var cur_square = si;

			// Maintain a list of the current square's units
			var cur_square_units = new List<List<string>>();

			// Look through the units, and see if the current square is in it,
			// and if so, add it to the list of of the square's units.
			foreach(var ui in units){
				var cur_unit = ui;

				if(cur_unit.IndexOf(cur_square) != -1){
					cur_square_units.Add(cur_unit);
				}
			}

			// Save the current square and its units to the map
			square_unit_map[cur_square] = cur_square_units;
		}

		return square_unit_map;
	}

	public Dictionary<string,List<string>> _get_square_peers_map(List<string> squares,Dictionary<string, List<List<string>>> units_map){
		/* Return a map of `squares` and their associated peers, i.e., a set of
		other squares in the square's unit.
		*/
		var square_peers_map = new Dictionary<string,List<string>>();

		// For every square...
		foreach(var si in squares){
			var cur_square = si;
			var cur_square_units = units_map[cur_square];

			// Maintain list of the current square's peers
			var cur_square_peers = new List<string>();

			// Look through the current square's units map...
			foreach(var sui in cur_square_units){
				var cur_unit = sui;

				foreach(var ui in cur_unit){
					var cur_unit_square = ui;

					if(cur_square_peers.IndexOf(cur_unit_square) == -1 &&
					                                                 cur_unit_square != cur_square){
						cur_square_peers.Add(cur_unit_square);
					}
				}
			}

			// Save the current square an its associated peers to the map
			square_peers_map[cur_square] = cur_square_peers;
		}

		return square_peers_map;
	}

	public bool validate_board(List<char> board){
		/* Return if the given `board` is valid or not. If it's valid, return
		true. If it's not, return a string of the reason why it's not.
		*/

		// Invalid board length
		if(board.Count != NR_SQUARES){
			//return "Invalid board size. Board must be exactly " + NR_SQUARES +
			//       " squares.";

			return false;
		}

		// Check for invalid characters
		foreach(var i in board){
			if(_in(i, DIGITS) == false && i != BLANK_CHAR){
				return false;
			}
		}

		// Otherwise, we're good. Return true.
		return true;
	}

	public bool _in(char v, string seq){
		/* Return if a value `v` is in sequence `seq`.
		*/
		return seq.IndexOf(v) != -1;
	}



	public Dictionary<string, char> _get_square_vals_map(List<char> board){
		/* Return a map of squares -> values
		*/
		var squares_vals_map = new Dictionary<string, char>();

		// Make sure `board` is a string of length 81
		if(board.Count != SQUARES.Count)
		{
			throw new Exception();

		} else {
			for (int i = 0; i < SQUARES.Count; i++)
			{
				squares_vals_map[SQUARES[i]] = board[i];
			}
		}

		return squares_vals_map;
	}


	public Dictionary<string, string> _assign(Dictionary<string, string> candidates, string square, char val){
		/* Eliminate all values, *except* for `val`, from `candidates` at
		`square` (candidates[square]), and propagate. Return the candidates map
		when finished. If a contradiciton is found, return false.

		WARNING: This will modify the contents of `candidates` directly.
		*/

		// Grab a list of canidates without 'val'
		var other_vals = candidates[square].Replace(val.ToString(), "".ToString());

		// Loop through all other values and eliminate them from the candidates
		// at the current square, and propigate. If at any point we get a
		// contradiction, return false.
		foreach(var ovi in other_vals){
			var other_val = ovi;

			var candidates_next = _eliminate(candidates, square, other_val);

			if(candidates_next == null){
				//console.log("Contradiction found by _eliminate.");
				return null;
			}
		}

		return candidates;
	}


	public Dictionary<string, string>  _eliminate(Dictionary<string, string> candidates, string square, char val){
        /* Eliminate `val` from `candidates` at `square`, (candidates[square]),
        and propagate when values or places <= 2. Return updated candidates,
        unless a contradiction is detected, in which case, return false.

        WARNING: This will modify the contents of `candidates` directly.
        */

        // If `val` has already been eliminated from candidates[square], return
        // with candidates.
        if(!_in(val, candidates[square])){
            return candidates;
        }

        // Remove `val` from candidates[square]
        candidates[square] = candidates[square].Replace(val.ToString(), "".ToString());

        // If the square has only candidate left, eliminate that value from its
        // peers
        var nr_candidates = candidates[square].Length;
        if(nr_candidates == 1){
            var target_val = candidates[square];

            foreach(var pi in SQUARE_PEERS_MAP[square]){
                var peer = pi;

                var candidates_new = _eliminate(candidates, peer, target_val[0]); //target_val[0] to turn it into a char since it's only supposed to be one character

                if(candidates_new == null){
                    return null;
                }
            }

        // Otherwise, if the square has no candidates, we have a contradiction.
        // Return false.
        } if(nr_candidates == 0){
            return null;
        }

        // If a unit is reduced to only one place for a value, then assign it
        foreach(var ui in SQUARE_UNITS_MAP[square]){
            var unit = ui;

            var val_places = new List<string>();
            foreach(var si in unit){
                var unit_square =si;
                if(_in(val, candidates[unit_square])){
                    val_places.Add(unit_square);
                }
            }

            // If there's no place for this value, we have a contradition!
            // return false
            if(val_places.Count == 0){
                return null;

            // Otherwise the value can only be in one place. Assign it there.
            } else if(val_places.Count == 1){
                var candidates_new = _assign(candidates, val_places[0], val);

                if(candidates_new == null){
                    return null;
                }
            }
        }

        return candidates;
    }


	public List<string> _shuffle(List<string> seq){
		/* Return a shuffled version of `seq`
		*/

		// Create an array of the same size as `seq` filled with false
		var shuffled = new List<string>();
		for(var i = 0; i < seq.Count; ++i){
			shuffled.Add("");
		}

		foreach(var i in seq){
			var ti = _rand_range(seq.Count);

			if (ti == seq.Count)
			{
				Loggy.LogError("AAA");
			}

			while(string.IsNullOrEmpty(shuffled[ti]) == false){
				ti = (ti + 1) > (seq.Count - 1) ? 0 : (ti + 1);
			}

			shuffled[ti] = i;
		}

		return shuffled;
	}

	public int _rand_range(int max, int min = 0 ){
		/* Get a random integer in the range of `min` to `max` (non inclusive).
		If `min` not defined, default to 0. If `max` not defined, throw an
		error.
		*/


		return Random.Range(min, max);

	}

	public List<string> _strip_dups(List<string> seq){
		/* Strip duplicate values from `seq`
		*/
		var seq_set = new List<string>();
		var dup_map = new Dictionary<string, bool>();
		foreach(var i in seq){
			var e = i;
			if(dup_map.ContainsKey(e) ==false || dup_map[e] == false){
				seq_set.Add(e);
				dup_map[e] = true;
			}
		}
		return seq_set;
	}

	public string solve(string board, bool reverse = false){
		/* Solve a sudoku puzzle given a sudoku `board`, i.e., an 81-character
		string of sudoku.DIGITS, 1-9, and spaces identified by '.', representing the
		squares. There must be a minimum of 17 givens. If the given board has no
		solutions, return false.

		Optionally set `reverse` to solve "backwards", i.e., rotate through the
		possibilities in reverse. Useful for checking if there is more than one
		solution.
		*/

		// Assure a valid board
		var report = validate_board(board.ToList());
		if(report != true)
		{
			return null;
		}

		// Check number of givens is at least MIN_GIVENS
		var nr_givens = 0;
		foreach(var i in board){
			if(i != BLANK_CHAR && _in(i, DIGITS)){
				++nr_givens;
			}
		}
		if(nr_givens < MIN_GIVENS)
		{
			return null;
			//throw new Exception("Too few givens. Minimum givens is " + MIN_GIVENS);
		}

		// Default reverse to false
		reverse = reverse || false;

		var candidates = _get_candidates_map(board.ToList());
		var result = _search(candidates, reverse);

		if(result != null){
			var solution = "";
			foreach(var square in result){
				solution += square;
			}
			return solution;
		}
		return null;
	}


	public Dictionary<string, string> _search(Dictionary<string, string> candidates, bool reverse = false){
        /* Given a map of squares -> candiates, using depth-first search,
        recursively try all possible values until a solution is found, or false
        if no solution exists.
        */

        // Return if error in previous iteration
        if(candidates == null){
            return null;
        }

        // If only one candidate for every square, we've a solved puzzle!
        // Return the candidates map.
        var max_nr_candidates = 0;
        string max_candidates_square = null;
        foreach(var si in SQUARES){
            var square = si;

            var nr_candidates = candidates[square].Length;

            if(nr_candidates > max_nr_candidates){
                max_nr_candidates = nr_candidates;
                max_candidates_square = square;
            }
        }
        if(max_nr_candidates == 1){
            return candidates;
        }

        // Choose the blank square with the fewest possibilities > 1
        var min_nr_candidates = 10;
        string min_candidates_square = null;
        foreach(var si in SQUARES){
            var square = si;

            var nr_candidates = candidates[square].Length;

            if(nr_candidates < min_nr_candidates && nr_candidates > 1){
                min_nr_candidates = nr_candidates;
                min_candidates_square = square;
            }
        }

        // Recursively search through each of the candidates of the square
        // starting with the one with fewest candidates.

        // Rotate through the candidates forwards
        var min_candidates = candidates[min_candidates_square];
        if(!reverse){
	        foreach(var vi in min_candidates){
                var val = vi;

                // TODO: Implement a non-rediculous deep copy function
                var candidates_copy = candidates.ToList().ToDictionary(x => x.Key, x => x.Value);

                var candidates_next = _search(_assign(candidates_copy, min_candidates_square, val));

                if(candidates_next != null){
                    return candidates_next;
                }
            }

        // Rotate through the candidates backwards
        } else {
            for(var vi = min_candidates.Length - 1; vi >= 0; --vi){
                var val = min_candidates[vi];

                // TODO: Implement a non-rediculous deep copy function
                var candidates_copy = candidates.ToList().ToDictionary(x => x.Key, x => x.Value);
                var candidates_next = _search(_assign(candidates_copy, min_candidates_square, val), reverse);

                if(candidates_next!= null ){
                    return candidates_next;
                }
            }
        }

        // If we get through all combinations of the square with the fewest
        // candidates without finding an answer, there isn't one. Return false.
        return null;
    }

}
