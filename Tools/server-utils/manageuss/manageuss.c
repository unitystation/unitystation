#define _GNU_SOURCE
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <unistd.h>
#include <ctype.h>

char *strlwr(char *input)
{
    size_t max = strlen(input);
    char *output = malloc((max + 1) * sizeof(char));
    for (int i = 0; i < max; i++)
    {
        char lower = tolower(input[i]);
        output[i] = lower;
    }
    output[max] = 0;
    return output;
}

int execute(char *cmd)
{
    environ = NULL;
    clearenv();
    char *method;
    char *lowered = strlwr(cmd + 1);
    asprintf(&method, "org.freedesktop.systemd1.Manager.%c%sUnit", toupper(cmd[0]), lowered);
    free(lowered);
    char *argv[8];
    argv[0] = "dbus-send";
    argv[1] = "--system";
    argv[2] = "--dest=org.freedesktop.systemd1";
    argv[3] = "/org/freedesktop/systemd1";
    argv[4] = method;
    argv[5] = "string:unitystation.service";
    argv[6] = "string:replace";
    argv[7] = NULL;

    pid_t cpid = fork();
    if (cpid == 0)
    {
        execvp("dbus-send", argv);
        return 1;
    }
    free(method);
}

int main(int argc, char *argv[])
{
    if (argc != 2)
    {
        printf("Usage: %s <start|stop|restart>\n", argv[0]);
        return 1;
    }
    if (strcmp(argv[1],"start") || strcmp(argv[1],"stop") || strcmp(argv[1],"restart"))
        return execute(argv[1]);
    else
    {
        printf("Usage: %s <start|stop|restart>\n", argv[0]);
        return 1;
    }
}
