#include <stdio.h>

// Function to calculate Annual Percentage Rate (APR)
double calculate_apr(double principal, double interest, int term_in_years) {
    return (interest / principal) / term_in_years * 100.0;
}

// Function to check if APR exceeds legal threshold
int is_usury(double apr, double threshold) {
    return apr > threshold;
}
void informIfUsury(double apr, double threshold){
	printf("APR is %.2f\n",apr);
	if (is_usury(apr, threshold)) {
        printf("⚠️ Usury detected! APR exceeds %.2f%% legal threshold.\n", threshold);
    } else {
        printf("✅ Loan is within legal interest limits.\n");
    }
}
int main() {
    double principal, interest, threshold;
    int term;

    // Example input
    principal = 1000.0;     // Loan amount
    interest = 400.0;       // Total interest charged
    term = 1;               // Loan term in years
    threshold = 20.0;       // Legal usury threshold (20% APR)

    double apr = calculate_apr(principal, interest, term);

    printf("Loan principal: %.2f\n", principal);
    printf("Total interest: %.2f\n", interest);
    printf("Term: %d year(s)\n", term);
    printf("APR: %.2f%%\n", apr);
	informIfUsury(apr,threshold);
    printf("Enter principal: ");
	scanf("%lf",&principal);
	printf("Enter interest: ");
	scanf("%lf",&interest);
	apr = calculate_apr(principal, interest, term);
	informIfUsury(apr,threshold);
    return 0;
}
