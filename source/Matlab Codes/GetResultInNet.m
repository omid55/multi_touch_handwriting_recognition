%Omid55
%Get Result In Neural Network

function [Z] = GetResultInNet( net )

Inp=[];
x=csvread('D:\X.txt');
Inp=[Inp;x'];
y=csvread('D:\Y.txt');
Inp=[Inp;y'];
p=csvread('D:\P.txt');
Inp=[Inp;p'];

y=sim(net,Inp);
%Z=sum(y)/length(y);   %  << CHECK HERE >> or Z=max(y);
Z=max(y);

end

