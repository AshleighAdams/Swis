// compile with clang using `clang++ -cc1 -triple=i386 -masm=intel -S program.cpp -emit-llvm`
// i386 is little endian and sizeof(void*) = 4

void reverse(char str[], int length)
{
	int start = 0;
	int end = length - 1;
	while (start < end)
	{
		// swap(*(str + start), *(str + end));
		char tmp = *(str + start);
		*(str + start) = *(str + end);
		*(str + end) = tmp;

		start++;
		end--;
	}
}

char* itoa(int num, char* str, int base)
{
	int i = 0;
	bool negative = false;

	// Handle 0 explicitely, otherwise empty string is printed for 0
	if (num == 0)
	{
		str[i++] = '0';
		str[i] = '\0';
		return str;
	}

	// In standard itoa(), negative numbers are handled only with 
	// base 10. Otherwise numbers are considered unsigned.
	if (num < 0 && base == 10)
	{
		negative = true;
		num = -num;
	}

	// Process individual digits
	while (num != 0)
	{
		int rem = num % base;
		str[i++] = (rem > 9) ? (rem - 10) + 'a' : rem + '0';
		num = num / base;
	}

	if (negative)
		str[i++] = '-';

	str[i] = '\0';
	reverse(str, i);
	return str;
}

void out(unsigned int port, unsigned char val)
{
	// https://gcc.gnu.org/onlinedocs/gcc/Simple-Constraints.html#Simple-Constraints
	// r = register, m = memory, o = offsetable memory, v = not-offsetable memory
	// i = immediate, g = register, memory, or immediate
	// X = any operand
	asm("out %0, %1"
		:                      // outputs
	: "X"(port), "X"(val)  // inputs
		: // valid registers
		);
}

void put(char c)
{
	asm("out 0, %0"
		:        // outputs
	: "X"(c) // inputs
		:        // valid registers
		);
}

void puts(const char* str)
{
	while (*str != 0)
	{
		out(0, *str);
		str++;
	}
}

/*
static unsigned long int next = 1;
int rand(void) // RAND_MAX assumed to be 32767
{
next = next * 1103515245 + 12345;
return (unsigned int)(next/65536) % 32768;
}
void srand(unsigned int seed)
{
next = seed;
}
*/

int main()
{
	char output[sizeof(int) * 8 + 1];
	for (int i = 1330; i < 1340; i++)
	{
		itoa(i, output, 10);
		puts(output);
		put('\n');
	}
	return 0;
}
