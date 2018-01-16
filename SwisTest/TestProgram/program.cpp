// compile with clang using `clang++ -cc1 -triple=i386 -S program.cpp -emit-llvm`
// i386 is little endian and sizeof(void*) = 4
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

short alloca_test(short value, short* other)
{
	short x = 5;
	if (value > x)
	{
		x = value * (*other);
	}
	return x;
}

int main()
{
	char msg[] = "Hello, world!\n";
	print(msg);
	return 0;
}