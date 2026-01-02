@echo off

del *.spv

pause

glslangValidator -V triangle.frag -o triangle.frag.spv

glslangValidator -V triangle.vert -o triangle.vert.spv

pause