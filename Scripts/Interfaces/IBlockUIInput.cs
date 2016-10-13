using System;

public interface IBlockUIInput
{
	Action<bool> setInputBlocked { set; }
}