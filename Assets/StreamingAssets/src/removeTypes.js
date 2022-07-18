const babel = require("@babel/core"); 
const preset = require("@babel/preset-typescript"); 

module.exports = function stripTypes(code) {
  return babel.transformSync(code, {
    filename: 'random.ts',
    presets: ["@babel/preset-typescript"],
    
  }).code;
}