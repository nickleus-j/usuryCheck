#include <stdio.h>
#include <string.h>

#define MAX_WORD_LENGTH 50

int main() {
    char word1[MAX_WORD_LENGTH], word2[MAX_WORD_LENGTH], word3[MAX_WORD_LENGTH];
    char *words[3];
    char *temp;
    int i, j;

    printf("Enter the first word: ");
    scanf("%s", word1);
    printf("Enter the second word: ");
    scanf("%s", word2);
    printf("Enter the third word: ");
    scanf("%s", word3);

    // Assign pointers to the input strings
    words[0] = word1;
    words[1] = word2;
    words[2] = word3;

    // Use the Bubble Sort algorithm to sort the words alphabetically
    for (i = 0; i < 2; i++) {
        for (j = i + 1; j < 3; j++) {
            // Compare the words using strcmp
            // If the first word is lexicographically greater than the second, swap them
            if (strcmp(words[i], words[j]) > 0) {
                temp = words[i];
                words[i] = words[j];
                words[j] = temp;
            }
        }
    }

    printf("\nWords in alphabetical order:\n");
    printf("%s\n", words[0]);
    printf("%s\n", words[1]);
    printf("%s\n", words[2]);

    return 0;
}

