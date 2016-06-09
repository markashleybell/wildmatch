#include <stddef.h>
#include "wildmatch.h"

main(int argc, char **argv)
{
    if(argc < 2) 
    {
        printf("%s\n", "Usage: wm <pattern> <text>");
        return 1;
    }

    int match = wildmatch(argv[1], argv[2], WM_PATHNAME, NULL);
    printf("%i\n", match);
}