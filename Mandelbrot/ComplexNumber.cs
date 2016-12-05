
    struct ComplexNumber
    {
        public double Re;
        public double Im;

        public ComplexNumber(double re, double im)
        {
            this.Re = re;
            this.Im = im;
        }

        public static ComplexNumber operator +(ComplexNumber x, ComplexNumber y)
        {
            return new ComplexNumber(x.Re + y.Re, x.Im + y.Im);
        }

        public static ComplexNumber operator *(ComplexNumber x, ComplexNumber y)
        {
            return new ComplexNumber(x.Re * y.Re - x.Im * y.Im,
                x.Re * y.Im + x.Im * y.Re);
        }
        public static ComplexNumber operator &(ComplexNumber x, ComplexNumber y)
        {
            return new ComplexNumber(x.Re * x.Re - x.Im * x.Im + y.Re,
                2 * x.Re * x.Im + y.Im);
        }

        public double Magnitude()
        {
            return Re * Re + Im * Im;
        }
    }

