﻿using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using Excel = Microsoft.Office.Interop.Excel;

namespace ExcelJsonEditorAddin.JsonTokenModel
{
    public class JsonObject : IJsonToken
    {
        private JObject _token;
        private Excel.Worksheet _sheet = null;
        private List<CellData> _cellDatas = new List<CellData>();

        private readonly int _titleRow = 0;

        public JsonTokenType Type() => _token.Type.ConvertToJsonTokenType();
        public JToken GetToken() => _token;

        public IEnumerable<JProperty> Keys => _cellDatas
            .Where(x => x.Type == DataType.Key)
            .Select(x => (JProperty)x.Key.GetToken());

        public JsonObject(JObject jObject)
        {
            _token = jObject;

            _cellDatas = MakeCellData(null, _token).ToList();
        }

        public void Dump(Excel.Worksheet sheet)
        {
            _sheet = sheet;
            //((Excel.Range)_sheet.Range[_titleRow, 1]).Value2 = "Key";
            //((Excel.Range)_sheet.Range[_titleRow, 2]).Value2 = "Value";

            _cellDatas = MakeCellData(_sheet, _token).ToList();
            SetNamedRange(_sheet, _cellDatas.Where(x => x.Type == DataType.Key));
            _cellDatas.ForEach(x => x.Value.Dump(x.Cell));
        }

        public void Dump(Excel.Range cell)
        {
            cell.Value2 = "{object}";
        }

        public bool OnDoubleClick(Excel.Range target)
        {
            return false;
        }

        public bool OnRightClick(Excel.Range target)
        {
            return false;
        }

        public void OnChangeValue(Excel.Range target)
        {
            var cellData = _cellDatas.FirstOrDefault(x => x.Address == target.Address);

            cellData?.Value.OnChangeValue(target);

            if (cellData?.Type == DataType.Key)
            {
                _cellDatas = MakeCellData(_sheet, _token).ToList();
            }
        }

        private IEnumerable<CellData> MakeCellData(Excel.Worksheet sheet, JObject token)
            => token.Properties()
                .Select((x, i) => new
                {
                    Index = i,
                    Property = x,
                    PropertyToken = x.CreateJsonToken(),
                    ValueToken = x.Value.CreateJsonToken(),
                })
                .SelectMany(x => new CellData[]
                {
                    new CellData
                    {
                        Type = DataType.Key,
                        Address = ((Excel.Range)sheet?.Cells[x.Index + _titleRow + 1, 1])?.Address,
                        Cell = (Excel.Range)sheet?.Cells[x.Index + _titleRow + 1, 1],
                        Key = x.PropertyToken,
                        Value = x.PropertyToken,
                    },
                    new CellData
                    {
                        Type = DataType.Value,
                        Address = ((Excel.Range)sheet?.Cells[x.Index + _titleRow + 1, 2])?.Address,
                        Cell = (Excel.Range)sheet?.Cells[x.Index + _titleRow + 1, 2],
                        Key = x.PropertyToken,
                        Value = x.ValueToken,
                    }
                });

        private void SetNamedRange(Excel.Worksheet sheet, IEnumerable<CellData> keys)
        {
            keys.ToList().ForEach(x =>
            {
                var propertyName = ((JProperty) x.Key.GetToken()).Name;
                sheet.Names.Add(propertyName, x.Cell.Offset[0, 1]);
            });
        }
    }
}
