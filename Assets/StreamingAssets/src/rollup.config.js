import { nodeResolve } from '@rollup/plugin-node-resolve';
import commonjs from '@rollup/plugin-commonjs';
import json from "@rollup/plugin-json"


export default {
  input: 'removeTypes.js',
  output: {
    file: 'bundle.js',
    format: 'iife',
    globals: {
      'tty': 'tty',
      'util': 'util',
      'os': 'os',
      'assert': 'assert',
      'path': 'path',
      'fs': 'fs',
      'url': 'url',
      'module': 'module',
      'buffer': 'buffer',
    }
  },
  plugins: [nodeResolve(), commonjs(), json()]
};