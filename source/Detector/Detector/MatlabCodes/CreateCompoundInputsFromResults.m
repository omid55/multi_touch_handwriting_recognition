% Omid55
% Create Compound Inputs From Results
resultInputs=[];
resultInputs=[resultInputs;sim(netX,inputsX)];
resultInputs=[resultInputs;sim(netY,inputsY)];
resultInputs=[resultInputs;sim(netP,inputsP)];
