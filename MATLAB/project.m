function f = project(a, b)
	f = dot(a, b) * b / (vectorlength(b) * vectorlength(b));
	% //Vector3 Fa = dampeningCoefficient * Vector3.Dot(A.velocity, r) * r / (Rm*Rm);
end