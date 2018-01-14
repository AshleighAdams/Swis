// compile with clang using `clang++ -cc1 -S program.cpp -emit-llvm`

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

int alloca_test(int value)
{
	int x = 5;
	if (value > x)
	{
		x = x * value;
	}
	return value;
}

int main()
{
	char msg[] = "Hello, world!\n";
	print(msg);
	return 0;
}