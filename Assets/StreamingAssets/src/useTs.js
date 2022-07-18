const ts = require("typescript");

function convert(source){
  const result = ts.transpileModule(source, { compilerOptions: {
    target: ts.ScriptTarget.ESNext,
    module: ts.ModuleKind.None,
    newLine: ts.NewLineKind.CarriageReturnLineFeed
  }});
  return result.outputText;
}

module.exports = convert;