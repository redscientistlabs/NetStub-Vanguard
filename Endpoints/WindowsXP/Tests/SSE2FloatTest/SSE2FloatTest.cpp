#include <conio.h>
#include <ctype.h>
#include <stdio.h>

int main(void) {
    printf("Waiting for input...\n");
    _getch();
    float one = 1;
    float two = 2;
    float three = 3;
    float four = 4;
    float five = 5;
    float six = 6;
    float onepointfive = 1.5;
    two = one + one;
    float twopointfive = 2.5;
    four = two * two;
    float threepointfive = 3.5;
    five = three + two;
    float onepointtwentyfive = 1.25;
    six = two * three;
    printf("1 + 1 = %.2f\n", two);
    printf("2 * 2 = %.2f\n", four);
    printf("3 + 2 = %.2f\n", five);
    printf("3 * 2 = %.2f\n", six);
    float test = four;
    float test2 = four * four;
    float test3 = four * four * four;
    do {
        printf("%.2f(%.2f + %.2f) = %.2f\n", test, test2, test3, (test * (test2 + test3)));
        test *= 2;
    } while (test < 65535);
    _getch();
    return 0;
}