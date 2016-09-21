using System;

[Flags]
public enum SelectionFlags
{
	Ray = 1 << 0,
	Direct = 1 << 1
}