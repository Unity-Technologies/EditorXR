using System;

public interface IBlockInput
{
	Action<bool> setInputBlocked { set; }
}