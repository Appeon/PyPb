using Python.Runtime;

namespace Appeon.OpenpyxlWrapper;

public class XlsxWriter
{
    private readonly PyModule _module;
    private readonly PyObject _workbook;
    private readonly PyObject _worksheet;
    private PyObject? _headerStyle;
    private PyObject? _rowStyle;
    private PyObject? _interleaveStyle;
    private int _lastRow;
    private bool isClosed;

    internal XlsxWriter(PyModule module, PyObject workbook, PyObject worksheet)
    {
        _module = module;
        _workbook = workbook;
        _worksheet = worksheet;
    }

    public int SetTheme(Theme t, out string? error)
    {
        error = null;

        try
        {
            CheckIsClosed();

            using var _ = Py.GIL();
            if (PyModule.Import("openpyxl.styles") is not PyModule stylesModule)
            {

                error = "Could not load openpyxl.styles";
                return -1;
            }

            PyDict locals = new();

            stylesModule.Exec(
$@"style = NamedStyle(name='{t.ThemeName + "Header"}')
color = Color(rgb='{t.HeaderBackgroundColor}')
font = Font(color=Color(rgb='{t.HeaderTextColor}'))
style.fill = PatternFill(patternType='solid', bgColor=color, fgColor=color, fill_type='solid')
style.font = font", locals);
            _headerStyle = locals["style"];

            stylesModule.Exec(
$@"style = NamedStyle(name='{t.ThemeName + "Row"}')
color = Color(rgb='{t.RowBackgroundColor}')
font = Font(color=Color(rgb='{t.RowTextColor}'))
style.fill = PatternFill(patternType='solid', bgColor=color, fgColor=color, fill_type='solid')
style.font = font", locals);
            _rowStyle = locals["style"];

            stylesModule.Exec(
$@"style = NamedStyle(name='{t.ThemeName + "Interleave"}')
color = Color(rgb='{t.InterleaveBackgroundColor}')
font = Font(color=Color(rgb='{t.InterleaveTextColor}'))
style.fill = PatternFill(patternType='solid', bgColor=color, fgColor=color, fill_type='solid')
style.font = font", locals);
            _interleaveStyle = locals["style"];
        }
        catch (Exception e)
        {
            error = "Failed to create styles from theme: " + e.Message;
            return -1;
        }

        return 0;
    }

    public int WriteRow(string[] data, bool isHeader, out string? error)
    {
        error = null;

        try
        {
            CheckIsClosed();

            using var _ = Py.GIL();

            using var scope = Py.CreateScope();
            var locals = new PyDict();

            ++_lastRow;
            locals["sheet"] = _worksheet;
            locals["y"] = (_lastRow).ToPython();
            locals["isHeader"] = isHeader.ToPython();
            for (int i = 0; i < data.Length; ++i)
            {
                locals["x"] = (i + 1).ToPython();
                locals["value"] = (data[i]).ToPython();
                if (_headerStyle is not null)
                    locals["headerStyle"] = _headerStyle;
                if (_rowStyle is not null)
                    locals["rowStyle"] = _rowStyle;
                if (_interleaveStyle is not null)
                    locals["interleaveRowStyle"] = _interleaveStyle;


                scope.Exec(
@"cell = sheet.cell(y, x, value)
if isHeader:
    cell.style = headerStyle
else:
    cell.style = rowStyle if y % 2 == 1 else interleaveRowStyle
", locals);
            }
        }
        catch (Exception e)
        {
            error = "Failed to write row: " + e.Message;
            return -1;
        }

        return 0;
    }

    public int WriteChart(string[] columns, string[] labels, decimal[] values, string chartOrigin, out string? error)
    {
        error = null;

        try
        {
            using var _ = Py.GIL();

            _module.Exec("from openpyxl.chart import PieChart, Reference");
            _module.Exec("from decimal import Decimal");

            var worksheet = _workbook.InvokeMethod("create_sheet");
            worksheet.InvokeMethod("append", columns.ToPyList());
            for (int i = 0; i < labels.Length; i++)
            {
                var devValue = _module.Eval($"Decimal({values[i]})");
                worksheet.InvokeMethod("append", new object[] { labels[i], devValue }.ToPyList());
            }



            var chart = _module.Eval("PieChart()");
            PyDict locals = new();
            locals["ws"] = worksheet;
            var labelsRef = _module.Eval($"Reference(ws, min_col=1, min_row=2, max_row={labels.Length + 1})", locals);
            var dataref = _module.Eval($"Reference(ws, min_col=2, min_row=1, max_row={values.Length + 1})", locals);
            locals.Clear();
            locals["pie"] = chart;
            locals["data"] = dataref;
            locals["labels"] = labelsRef;
            _module.Eval("pie.add_data(data, titles_from_data=True)", locals);
            _module.Eval("pie.set_categories(labels)", locals);
            chart.SetAttr("title", "Payroll expenditure by department".ToPython());

            _worksheet.InvokeMethod("add_chart", chart, chartOrigin.ToPython());
        }
        catch (Exception e)
        {
            error = e.Message;
            return -1;
        }


        return 0;
    }

    public int Autosize(out string? error)
    {
        error = null;

        try
        {
            CheckIsClosed();

            using var _ = Py.GIL();

            PyDict locals = new();
            locals["sheet"] = _worksheet;
            using var scope = Py.CreateScope();

            scope.Exec(
@"for col in sheet.columns:
    max_length = 0
    column = col[0].column_letter # Get the column name
    for cell in col:
        try: # Necessary to avoid error on empty cells
            if len(str(cell.value)) > max_length:
                max_length = len(str(cell.value))
        except:
            pass
    adjusted_width = (max_length + 2) 
    sheet.column_dimensions[column].width = adjusted_width", locals);
        }
        catch (Exception e)
        {
            error = "Failed to autosize columns: " + e.Message;
            return -1;
        }

        return 0;
    }


    public int Save(string path, out string? error)
    {
        error = null;

        try
        {
            CheckIsClosed();
            using var _ = Py.GIL();

            _workbook.InvokeMethod("save", path.ToPython());
            isClosed = true;
        }
        catch (Exception e)
        {
            error = "Failed to write to disk: " + e.Message;
            return -1;
        }

        return 0;

    }

    private void CheckIsClosed()
    {
        if (isClosed)
            throw new Exception("Writer is already closed");
    }
}
