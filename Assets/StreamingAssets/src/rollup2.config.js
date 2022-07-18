import { nodeResolve } from '@rollup/plugin-node-resolve';
import commonjs from '@rollup/plugin-commonjs';
import json from "@rollup/plugin-json"


export default {
  input: 'useTs.js',
  output: {
    file: 'ts-bundle.js',
    format: 'iife',
    globals: {
    }
  },
  plugins: [nodeResolve(), commonjs(), json()]
};