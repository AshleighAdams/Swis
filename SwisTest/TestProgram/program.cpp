void out(unsigned int port, unsigned char val)
{
	asm volatile ("out %0, %1" : : "r"(port), "r"(val));
}

void print(char* str)
{
	while (*str != 0)
	{
		out(0, *str);
		str++;
	}
}

int main()
{
	char msg[] = "Hello, world!\n";
	print(msg);
	return 0;
}