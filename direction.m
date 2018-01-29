function f = direction(a, b)
	d = dist(a, b);
	f = (b - a) * (1/d);
end