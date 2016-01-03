% Omid55
% My Simulations In Matlab Codes
xRes=sim(netX,testInputX);
yRes=sim(netY,testInputY);
pRes=sim(netP,testInputP);
allRes=[xRes;yRes;pRes];
myResult=sim(netResult,allRes);
