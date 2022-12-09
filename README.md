# CCFlow
heres my attempt at a control flow (cflow) cleaner, don't expect much from it

# Notes
- This is not a magic tool (far from it, you will likely need to clean some stuff yourself), if anyone want to help you can contact me or fork the repo
- This currently only support very simple cflow obfuscation (see Examples below)
- Both the cleaned code & the original cflow will be present in the method(s) that were cleaned, there is a `ret` instruction that seperates them aswell as a bunch of `ldstr` to let you know which is which
![split](https://i.imgur.com/B4ExFkj.png)
- the sorted methods are also saved as text files in the `log` folder

# Usage
Drag & Drop to Clean

# Examples
Control Flow Obfuscation:
<pre lang=csharp>
int num = 1;
for (;;)
{
  if (num == 3)
  {
    Console.WriteLine("Hello World!");
  }
  if (num == 1)
  {
    num = 2;
  }
  if (num == 2)
  {
    num = 3;
  }
}
</pre>

Expected result when cleaned with CCFlow:
<pre lang=csharp>
Console.WriteLine("Hello World!");
</pre>
