
p1 = [ 0  0  0 ];
p2 = [ 0 -1  0 ];
p3 = [ 0 -2  0 ];

m1 = 0.1;
m2 = 0.1;

m3 = 0.1;

d1_2 = dist(p1, p2);
d2_3 = dist(p2, p3);

timeStep = 0.016;
simStep = 0.005;
remainder = 0;

simTime = 1;

timeSteps = floor(simTime / timeStep);

for i = 1:timeSteps
	dt = timeStep+remainder;
	simSteps = floor(dt / simStep);
	remainder = dt - simSteps*simStep;
	
	for u = 1:simSteps
		p3 = p3 + [0 -9.82 * simStep, 0];
	end
	
	
	plot( p3(1), p3(2), 'o' );
	axis([-1 1 -5 1]);
	
	pause(timeStep)
end