- Use an IDE that respects `.editorconfig` file (http://editorconfig.org/#download) and you won't have to worry about most of the stuff below :smirk:

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

<img width="307" alt="screen shot 2017-10-25 at 2 09 16 pm" src="https://user-images.githubusercontent.com/23202691/32020662-31ed7d82-b98e-11e7-8705-a3016d7f038b.png">

__Example IDE settings (captured from Xamarin)__

