- Use tabs, not spaces, for indentation.

- Private fields should be prefixed with an underscore (`_privateField`) and generally avoid `this` keyword.

- Use verbose variable names. Do not abbreviate.

- Braces even for one liners:

  **RIGHT**
  ```cs
  if(condition) 
  {
      doStuff();
  }
  ```

  **WRONG**
  ```cs
  if(condition)
      doStuff();
  ```

- Braces on a new line:

  **RIGHT**
  ```cs
  if(condition) 
  {
      doStuff();
  }
  ```

  **WRONG**
  ```cs
  if(condition){
      doStuff();
  }
  ```

- Prefer good variable and method naming over comments. The exception is using comments for documentation purposes or complex algorithms.
