CC=mcs
CFLAGS=-r:System.Drawing
SRCS=NestFigure.cs NestPoint.cs Program.cs
BIN=figread

all:
	$(CC) $(CFLAGS) -out:$(BIN) $(SRCS)
