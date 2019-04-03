short stdout_line = 0;

void put(char c, short line)
{
	asm("out %0, %1"
		:                   // outputs
		: "X"(line), "X"(c) // inputs
		:                   // valid registers
		);
}

void puts(const char* str)
{
	short line = stdout_line;
	while (*str)
	{
		put(*str, line);
		str++;
	}
	put('\n', line);
}