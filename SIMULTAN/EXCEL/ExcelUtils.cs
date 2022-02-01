using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Excel
{
    static class ExcelUtils
    {
        #region TRANSLATION OF RANGES
        internal static (string range_start, string range_end) TranslateRange(int _start_row, int _nr_rows, int _start_col, int _nr_cols)
        {
            string range_start = LettersFromAnyNr(_start_col) + _start_row.ToString();
            string range_end = LettersFromAnyNr(_start_col + _nr_cols - 1) + (_start_row + _nr_rows - 1).ToString();
            return (range_start, range_end);
        }

        internal static string LettersFromAnyNr(int _nr)
        {
            List<int> figures = new List<int>();
            int figure = _nr;
            while (figure > 26)
            {
                int figure_new = figure / 26;
                int rest = figure % 26;
                if (rest == 0)
                    figure_new--;
                figure = figure_new;
                figures.Add(rest);
            }
            figures.Add(figure);
            figures.Reverse();

            if (figures.Count <= 1)
                return LetterFromNr(figures[0]);
            else
                return figures.Select(x => LetterFromNr(x)).Aggregate((x, y) => x + y);
        }



        private static string LetterFromNr(int _nr)
        {
            string letter = "A";
            switch (_nr)
            {
                case 1:
                    letter = "A";
                    break;
                case 2:
                    letter = "B";
                    break;
                case 3:
                    letter = "C";
                    break;
                case 4:
                    letter = "D";
                    break;
                case 5:
                    letter = "E";
                    break;
                case 6:
                    letter = "F";
                    break;
                case 7:
                    letter = "G";
                    break;
                case 8:
                    letter = "H";
                    break;
                case 9:
                    letter = "I";
                    break;
                case 10:
                    letter = "J";
                    break;
                case 11:
                    letter = "K";
                    break;
                case 12:
                    letter = "L";
                    break;
                case 13:
                    letter = "M";
                    break;
                case 14:
                    letter = "N";
                    break;
                case 15:
                    letter = "O";
                    break;
                case 16:
                    letter = "P";
                    break;
                case 17:
                    letter = "Q";
                    break;
                case 18:
                    letter = "R";
                    break;
                case 19:
                    letter = "S";
                    break;
                case 20:
                    letter = "T";
                    break;
                case 21:
                    letter = "U";
                    break;
                case 22:
                    letter = "V";
                    break;
                case 23:
                    letter = "W";
                    break;
                case 24:
                    letter = "X";
                    break;
                case 25:
                    letter = "Y";
                    break;
                case 0:
                case 26:
                    letter = "Z";
                    break;
            }
            return letter;
        }

        #endregion
    }
}
