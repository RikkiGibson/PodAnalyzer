# Plain Old Data Analyzer
[![Build Status](https://travis-ci.org/RikkiGibson/PodAnalyzer.svg?branch=master)](https://travis-ci.org/RikkiGibson/PodAnalyzer)

This package contains Roslyn analyzers and code fixes intended to help you work with immutable data types in C#. The supported features include:

- Generate a constructor with parameters based on the auto-properties present in a type
- Transform an object initializer block to a constructor call
- Warn when a getter-only property is assigned to itself in a constructor or never assigned

This tool is meant to reduce the amount of boilerplate you have to write with your immutable data types while helping you ensure correctness as your types evolve over time. I'm considering what other features could be helpful for people wanting to create immutable data types, such as automatic generation of Builder classes or generation of `.With` methods.

Here is some new content.
